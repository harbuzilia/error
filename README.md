# YOLOv8 Training Baseline

This repository now contains a fully reproducible Ultralytics YOLOv8 workflow that trains a
lightweight detector on the bundled 17-image / 8-image synthetic dataset. The
pipeline is driven entirely by versioned configuration files so future runs can
be reproduced bit-for-bit.

## Repository layout

| Path | Purpose |
| --- | --- |
| `configs/dataset.yaml` | Dataset description with class names, formats, and relative roots |
| `configs/hyp/small_dataset.yaml` | Hyperparameter overrides tuned for tiny datasets |
| `configs/train.yaml` | Source of truth for model selection, paths, and runtime knobs |
| `data/raw/*` | 17 train / 8 val synthetic samples (images + YOLO labels) |
| `scripts/generate_synthetic_dataset.py` | Utility used to (re)build the dataset |
| `src/cli.py` | Minimal CLI: `python -m src.cli train --config <file>` |
| `runs/train/exp` | Captured training artifacts from the verification run |
| `reports/training-report.md` | Human-readable summary of the verified run |
| `reports/training-results.csv` | Raw per-epoch metrics from Ultralytics |
| `artifacts/best.pt` | Copy of the best checkpoint produced during verification |

## Environment setup

```bash
python -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
```

## Working configuration

The canonical experiment is defined in `configs/train.yaml`. It references the
synthetic dataset, the `yolov8n` base model, and `configs/hyp/small_dataset.yaml`
for learning rate and augmentation tweaks.

Command:

```bash
python -m src.cli train --config configs/train.yaml
```

Expected console tail:

```
Class     Images  Instances      Box(P          R      mAP50  m
all          8         17     0.0279      0.938      0.733  0.651
square_target          7          9    0.00397          1       0.62  0.555
circle_target          6          8     0.0519      0.875      0.847  0.746
Results saved to /home/engine/project/runs/train/exp
```

Outputs:
- `runs/train/exp`: complete Ultralytics run directory (plots, metrics, weights)
- `reports/training-report.md`: summarized dataset + hyperparameters + final mAP
- `reports/training-results.csv`: exact per-epoch metrics (mirrors `results.csv`)
- `artifacts/best.pt`: handy copy of the best-performing checkpoint

## Make-based shortcut

```bash
PYTHON=.venv/bin/python make train
```

If `PYTHON` is omitted, `make` uses the system interpreter.

## Dataset generation

`scripts/generate_synthetic_dataset.py` can rebuild the dataset at any time. The
script produces 320×320 JPGs containing the two target classes:
`square_target` and `circle_target`. The generated structure matches the paths
hard-coded in `configs/dataset.yaml`.

## Reporting and metrics

The verification run (8 epochs, batch size 8, 320px images, AdamW optimizer) is
captured in `reports/training-report.md`. Final metrics:

- Precision: **0.012**
- Recall: **1.000**
- mAP@50: **0.706**
- mAP@50-95: **0.550**

Any future run of `python -m src.cli train --config configs/train.yaml` should
regenerate the same artifacts and update the report/CSV in-place, ensuring the
history stays reproducible.
