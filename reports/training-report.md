# Training Report

## Dataset
- Root: `data/raw`
- Train images: 17 (data/raw/train/images)
- Val images: 8 (data/raw/val/images)
- Classes: square_target, circle_target
- Image formats: jpg
- Notes: Synthetic dataset with 17 training images and 8 validation images containing square and circle primitives on noisy backgrounds.

## Hyperparameters
- Model: yolov8n.pt
- Epochs: 8 | Batch: 8 | Image size: 320
- Optimizer: AdamW | Patience: 5 | Workers: 0
- Device: cpu | Seed: 42
- Augmentation preset: lightweight

## Final Metrics
- Epoch: 8
- Precision: 0.0120
- Recall: 1.0000
- mAP@50: 0.7062
- mAP@50-95: 0.5504

## Reproduction
Run the following command from the repository root:

```bash
python -m src.cli train --config configs/train.yaml
```

Artifacts:
- `artifacts/best.pt` (best checkpoint)
- `reports/training-results.csv` (per-epoch metrics)