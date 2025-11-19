"""Simple CLI wrapper around the Ultralytics YOLOv8 training API.

The command currently exposes one sub-command:

```
python -m src.cli train --config configs/train.yaml
```
"""

from __future__ import annotations

import argparse
import csv
import shutil
from dataclasses import dataclass
from pathlib import Path
from typing import Any, Dict, Iterable, List

import yaml
from ultralytics import YOLO


def load_yaml(path: Path) -> Dict[str, Any]:
    with path.open("r", encoding="utf-8") as handle:
        return yaml.safe_load(handle) or {}


def resolve_path(value: str | None, base_dir: Path) -> Path | None:
    if value is None:
        return None
    candidate = Path(value)
    if not candidate.is_absolute():
        candidate = (base_dir / candidate).resolve()
    return candidate


def count_images(dir_path: Path) -> int:
    if not dir_path.exists():
        return 0
    exts = {".jpg", ".jpeg", ".png", ".bmp"}
    return sum(1 for file in dir_path.iterdir() if file.suffix.lower() in exts)


def dataset_root_from_value(dataset_cfg_path: Path, value: str | Path | None) -> Path:
    if value is None:
        return dataset_cfg_path.parent
    candidate = Path(value)
    if candidate.is_absolute():
        return candidate
    search_bases: List[Path] = [dataset_cfg_path.parent]
    search_bases.extend(list(dataset_cfg_path.parents[1:3]))
    for base in search_bases:
        if base is None:
            continue
        resolved = (base / candidate).resolve()
        if resolved.exists():
            return resolved
    return (dataset_cfg_path.parent / candidate).resolve()


@dataclass
class DatasetStats:
    root: Path
    train_images: Path
    val_images: Path
    train_count: int
    val_count: int
    class_names: List[str]
    image_formats: List[str]
    description: str | None = None


def collect_dataset_stats(dataset_cfg_path: Path) -> DatasetStats:
    cfg = load_yaml(dataset_cfg_path)
    path_value = cfg.get("path", ".")
    root = dataset_root_from_value(dataset_cfg_path, path_value)

    train_rel = cfg.get("train", "train/images")
    val_rel = cfg.get("val", "val/images")
    train_images = (root / train_rel).resolve()
    val_images = (root / val_rel).resolve()

    names_raw = cfg.get("names", [])
    if isinstance(names_raw, dict):
        names = [names_raw[key] for key in sorted(names_raw.keys(), key=lambda item: int(item))]
    else:
        names = list(names_raw)

    meta = cfg.get("meta", {})
    formats = meta.get("formats", {})
    image_formats = formats.get("images") if isinstance(formats, dict) else None
    description = meta.get("description") if isinstance(meta, dict) else None

    return DatasetStats(
        root=root,
        train_images=train_images,
        val_images=val_images,
        train_count=count_images(train_images),
        val_count=count_images(val_images),
        class_names=names,
        image_formats=list(image_formats) if image_formats else ["jpg"],
        description=description,
    )


def parse_results(results_csv: Path) -> Dict[str, float | int]:
    if not results_csv.exists():
        return {}
    with results_csv.open("r", encoding="utf-8") as handle:
        reader = list(csv.DictReader(handle))
    if not reader:
        return {}
    final_row = reader[-1]

    def _as_float(key: str) -> float:
        value = final_row.get(key)
        try:
            return float(value) if value not in (None, "") else 0.0
        except ValueError:
            return 0.0

    metrics = {
        "epoch": int(float(final_row.get("epoch", 0) or 0)),
        "precision": _as_float("metrics/precision(B)"),
        "recall": _as_float("metrics/recall(B)"),
        "map50": _as_float("metrics/mAP50(B)"),
        "map50_95": _as_float("metrics/mAP50-95(B)"),
    }
    return metrics


def write_report(report_path: Path, stats: DatasetStats, training_cfg: Dict[str, Any], metrics: Dict[str, float | int]) -> None:
    lines: List[str] = []
    lines.append("# Training Report")
    lines.append("")
    lines.append("## Dataset")
    try:
        repo_root = report_path.parents[1]
    except IndexError:
        repo_root = report_path.parent
    if repo_root and (stats.root == repo_root or repo_root in stats.root.parents):
        root_repr = stats.root.relative_to(repo_root)
    else:
        root_repr = stats.root
    lines.append(f"- Root: `{root_repr}`")

    def _fmt_path(path: Path) -> str:
        if repo_root and (path == repo_root or repo_root in path.parents):
            return str(path.relative_to(repo_root))
        return str(path)

    lines.append(f"- Train images: {stats.train_count} ({_fmt_path(stats.train_images)})")
    lines.append(f"- Val images: {stats.val_count} ({_fmt_path(stats.val_images)})")
    if stats.class_names:
        lines.append(f"- Classes: {', '.join(stats.class_names)}")
    if stats.image_formats:
        lines.append(f"- Image formats: {', '.join(stats.image_formats)}")
    if stats.description:
        lines.append(f"- Notes: {stats.description}")
    lines.append("")
    lines.append("## Hyperparameters")
    lines.append(f"- Model: {training_cfg['model']}" )
    lines.append(f"- Epochs: {training_cfg['epochs']} | Batch: {training_cfg['batch']} | Image size: {training_cfg['imgsz']}")
    lines.append(f"- Optimizer: {training_cfg['optimizer']} | Patience: {training_cfg['patience']} | Workers: {training_cfg['workers']}")
    lines.append(f"- Device: {training_cfg.get('device', 'cpu')} | Seed: {training_cfg.get('seed', 'n/a')}")
    if training_cfg.get("augmentation_profile"):
        lines.append(f"- Augmentation preset: {training_cfg['augmentation_profile']}")
    lines.append("")
    lines.append("## Final Metrics")
    if metrics:
        lines.append(f"- Epoch: {metrics.get('epoch', 'n/a')}")
        lines.append(f"- Precision: {metrics.get('precision', 0):.4f}")
        lines.append(f"- Recall: {metrics.get('recall', 0):.4f}")
        lines.append(f"- mAP@50: {metrics.get('map50', 0):.4f}")
        lines.append(f"- mAP@50-95: {metrics.get('map50_95', 0):.4f}")
    else:
        lines.append("- Metrics not available (results.csv missing)")
    lines.append("")
    lines.append("## Reproduction")
    lines.append("Run the following command from the repository root:")
    lines.append("\n```bash\npython -m src.cli train --config configs/train.yaml\n```\n")
    lines.append("Artifacts:")
    lines.append("- `artifacts/best.pt` (best checkpoint)")
    lines.append("- `reports/training-results.csv` (per-epoch metrics)")
    report_path.write_text("\n".join(lines), encoding="utf-8")


def train_command(config_path: Path) -> None:
    config = load_yaml(config_path)
    base_dir = config_path.parent

    dataset_cfg_path = resolve_path(config.get("dataset_config"), base_dir)
    if dataset_cfg_path is None or not dataset_cfg_path.exists():
        raise FileNotFoundError(f"Dataset config not found: {config.get('dataset_config')}")

    hyp_cfg_path = resolve_path(config.get("hyp_config"), base_dir)

    experiment_cfg = config.get("experiment", {})
    project_dir = resolve_path(experiment_cfg.get("project_dir", "runs/train"), base_dir)
    reports_dir = resolve_path(experiment_cfg.get("reports_dir", "reports"), base_dir)
    artifacts_dir = resolve_path(experiment_cfg.get("artifacts_dir", "artifacts"), base_dir)
    best_weights_path = resolve_path(experiment_cfg.get("best_weights"), base_dir)
    metrics_csv_target = resolve_path(experiment_cfg.get("metrics_csv"), base_dir)
    summary_md = resolve_path(experiment_cfg.get("summary_md"), base_dir)

    for directory in filter(None, [project_dir, reports_dir, artifacts_dir, summary_md and summary_md.parent, metrics_csv_target and metrics_csv_target.parent]):
        Path(directory).mkdir(parents=True, exist_ok=True)

    model_cfg = config.get("model", {})
    model_path = model_cfg.get("base", "yolov8n.pt")
    model = YOLO(model_path)

    training_cfg = config.get("training", {})
    callbacks_cfg = config.get("callbacks", {})

    train_kwargs: Dict[str, Any] = {
        "data": str(dataset_cfg_path),
        "epochs": training_cfg.get("epochs", 10),
        "batch": training_cfg.get("batch", 8),
        "imgsz": training_cfg.get("imgsz", 640),
        "patience": training_cfg.get("patience", 5),
        "optimizer": training_cfg.get("optimizer", "SGD"),
        "workers": training_cfg.get("workers", 0),
        "device": training_cfg.get("device", "cpu"),
        "seed": training_cfg.get("seed", 0),
        "verbose": training_cfg.get("verbose", True),
        "project": str(project_dir) if project_dir else "runs/train",
        "name": experiment_cfg.get("name", "exp"),
        "exist_ok": True,
        "plots": True,
    }

    if training_cfg.get("deterministic"):
        train_kwargs["deterministic"] = True

    if callbacks_cfg.get("save_pr_curve"):
        train_kwargs["plots"] = True
    if callbacks_cfg.get("enable_early_stopping"):
        train_kwargs["patience"] = training_cfg.get("patience", 5)

    if hyp_cfg_path and hyp_cfg_path.exists():
        train_kwargs.update(load_yaml(hyp_cfg_path))

    results = model.train(**train_kwargs)
    # The trainer keeps track of save_dir with all artifacts.
    save_dir = Path(model.trainer.save_dir)
    weights_dir = save_dir / "weights"
    best_weights_src = weights_dir / "best.pt"

    if best_weights_src.exists():
        target = best_weights_path or (artifacts_dir / "best.pt")
        shutil.copy2(best_weights_src, target)
    results_csv_path = save_dir / "results.csv"
    if results_csv_path.exists() and metrics_csv_target:
        shutil.copy2(results_csv_path, metrics_csv_target)

    stats = collect_dataset_stats(dataset_cfg_path)
    metrics = parse_results(results_csv_path) if results_csv_path.exists() else {}
    summary_target = summary_md or (reports_dir / "training-report.md")
    write_report(
        summary_target,
        stats,
        {
            "model": model_path,
            "epochs": train_kwargs["epochs"],
            "batch": train_kwargs["batch"],
            "imgsz": train_kwargs["imgsz"],
            "optimizer": train_kwargs["optimizer"],
            "patience": train_kwargs["patience"],
            "workers": train_kwargs["workers"],
            "device": train_kwargs.get("device", "cpu"),
            "seed": train_kwargs.get("seed"),
            "augmentation_profile": training_cfg.get("augmentation_profile"),
        },
        metrics,
    )


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Utility commands for the YOLOv8 training pipeline")
    subparsers = parser.add_subparsers(dest="command", required=True)

    train_parser = subparsers.add_parser("train", help="Run a full training session using a YAML config")
    train_parser.add_argument("--config", type=Path, required=True, help="Path to the training config YAML file")

    return parser


def main(argv: Iterable[str] | None = None) -> None:
    parser = build_parser()
    args = parser.parse_args(list(argv) if argv is not None else None)

    if args.command == "train":
        train_command(args.config.resolve())
    else:
        parser.error("Unknown command")


if __name__ == "__main__":
    main()
