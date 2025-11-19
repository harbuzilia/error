PYTHON ?= python

.PHONY: train
train:
	$(PYTHON) -m src.cli train --config configs/train.yaml
