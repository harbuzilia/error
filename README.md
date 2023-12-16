C:\Users\harb\Downloads\TrainingFolder>yolo task=detect mode=train imgsz=640 data=data.yaml epochs=100 batch=16 name=wf
Ultralytics YOLOv8.0.227 🚀 Python-3.11.0 torch-2.1.2+cu121 CUDA:0 (NVIDIA GeForce RTX 2060, 6144MiB)
engine\trainer: task=detect, mode=train, model=yolov8n.pt, data=data.yaml, epochs=100, patience=50, batch=16, imgsz=640, save=True, save_period=-1, cache=False, device=None, workers=8, project=None, name=wf9, exist_ok=False, pretrained=True, optimizer=auto, verbose=True, seed=0, deterministic=True, single_cls=False, rect=False, cos_lr=False, close_mosaic=10, resume=False, amp=True, fraction=1.0, profile=False, freeze=None, overlap_mask=True, mask_ratio=4, dropout=0.0, val=True, split=val, save_json=False, save_hybrid=False, conf=None, iou=0.7, max_det=300, half=False, dnn=False, plots=True, source=None, vid_stride=1, stream_buffer=False, visualize=False, augment=False, agnostic_nms=False, classes=None, retina_masks=False, show=False, save_frames=False, save_txt=False, save_conf=False, save_crop=False, show_labels=True, show_conf=True, show_boxes=True, line_width=None, format=torchscript, keras=False, optimize=False, int8=False, dynamic=False, simplify=False, opset=None, workspace=4, nms=False, lr0=0.01, lrf=0.01, momentum=0.937, weight_decay=0.0005, warmup_epochs=3.0, warmup_momentum=0.8, warmup_bias_lr=0.1, box=7.5, cls=0.5, dfl=1.5, pose=12.0, kobj=1.0, label_smoothing=0.0, nbs=64, hsv_h=0.015, hsv_s=0.7, hsv_v=0.4, degrees=0.0, translate=0.1, scale=0.5, shear=0.0, perspective=0.0, flipud=0.0, fliplr=0.5, mosaic=1.0, mixup=0.0, copy_paste=0.0, cfg=None, tracker=botsort.yaml, save_dir=runs\detect\wf9
Overriding model.yaml nc=80 with nc=1

                   from  n    params  module                                       arguments
  0                  -1  1       464  ultralytics.nn.modules.conv.Conv             [3, 16, 3, 2]
  1                  -1  1      4672  ultralytics.nn.modules.conv.Conv             [16, 32, 3, 2]
  2                  -1  1      7360  ultralytics.nn.modules.block.C2f             [32, 32, 1, True]
  3                  -1  1     18560  ultralytics.nn.modules.conv.Conv             [32, 64, 3, 2]
  4                  -1  2     49664  ultralytics.nn.modules.block.C2f             [64, 64, 2, True]
  5                  -1  1     73984  ultralytics.nn.modules.conv.Conv             [64, 128, 3, 2]
  6                  -1  2    197632  ultralytics.nn.modules.block.C2f             [128, 128, 2, True]
  7                  -1  1    295424  ultralytics.nn.modules.conv.Conv             [128, 256, 3, 2]
  8                  -1  1    460288  ultralytics.nn.modules.block.C2f             [256, 256, 1, True]
  9                  -1  1    164608  ultralytics.nn.modules.block.SPPF            [256, 256, 5]
 10                  -1  1         0  torch.nn.modules.upsampling.Upsample         [None, 2, 'nearest']
 11             [-1, 6]  1         0  ultralytics.nn.modules.conv.Concat           [1]
 12                  -1  1    148224  ultralytics.nn.modules.block.C2f             [384, 128, 1]
 13                  -1  1         0  torch.nn.modules.upsampling.Upsample         [None, 2, 'nearest']
 14             [-1, 4]  1         0  ultralytics.nn.modules.conv.Concat           [1]
 15                  -1  1     37248  ultralytics.nn.modules.block.C2f             [192, 64, 1]
 16                  -1  1     36992  ultralytics.nn.modules.conv.Conv             [64, 64, 3, 2]
 17            [-1, 12]  1         0  ultralytics.nn.modules.conv.Concat           [1]
 18                  -1  1    123648  ultralytics.nn.modules.block.C2f             [192, 128, 1]
 19                  -1  1    147712  ultralytics.nn.modules.conv.Conv             [128, 128, 3, 2]
 20             [-1, 9]  1         0  ultralytics.nn.modules.conv.Concat           [1]
 21                  -1  1    493056  ultralytics.nn.modules.block.C2f             [384, 256, 1]
 22        [15, 18, 21]  1    751507  ultralytics.nn.modules.head.Detect           [1, [64, 128, 256]]
Model summary: 225 layers, 3011043 parameters, 3011027 gradients, 8.2 GFLOPs

Transferred 319/355 items from pretrained weights
Freezing layer 'model.22.dfl.conv.weight'
AMP: running Automatic Mixed Precision (AMP) checks with YOLOv8n...
AMP: checks passed ✅
train: Scanning C:\Users\harb\Downloads\TrainingFolder\labels\train.cache... 17 images, 0 backgrounds, 0 corrupt: 100%|
val: Scanning C:\Users\harb\Downloads\TrainingFolder\labels\val.cache... 8 images, 0 backgrounds, 0 corrupt: 100%|█████
Traceback (most recent call last):
  File "<string>", line 1, in <module>
Traceback (most recent call last):
  File "<string>", line 1, in <module>
  File "C:\Users\harb\AppData\Local\Programs\Python\Python311\Lib\multiprocessing\spawn.py", line 120, in spawn_main
    exitcode = _main(fd, parent_sentinel)
               ^^^^^^^^^^^^^^^^^^^^^^^^^^
  File "C:\Users\harb\AppData\Local\Programs\Python\Python311\Lib\multiprocessing\spawn.py", line 120, in spawn_main
    exitcode = _main(fd, parent_sentinel)
               ^^^^^^^^^^^^^^^^^^^^^^^^^^
  File "C:\Users\harb\AppData\Local\Programs\Python\Python311\Lib\multiprocessing\spawn.py", line 130, in _main
    self = reduction.pickle.load(from_parent)
           ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
  File "C:\Users\harb\AppData\Local\Programs\Python\Python311\Lib\site-packages\torch\__init__.py", line 128, in <module>
    raise err
OSError: [WinError 1114] Произошел сбой в программе инициализации библиотеки динамической компоновки (DLL). Error loading "C:\Users\harb\AppData\Local\Programs\Python\Python311\Lib\site-packages\torch\lib\nvfuser_codegen.dll" or one of its dependencies.
  File "C:\Users\harb\AppData\Local\Programs\Python\Python311\Lib\multiprocessing\spawn.py", line 130, in _main
    self = reduction.pickle.load(from_parent)
           ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
  File "C:\Users\harb\AppData\Local\Programs\Python\Python311\Lib\site-packages\torch\__init__.py", line 128, in <module>
    raise err
OSError: [WinError 1114] Произошел сбой в программе инициализации библиотеки динамической компоновки (DLL). Error loading "C:\Users\harb\AppData\Local\Programs\Python\Python311\Lib\site-packages\torch\lib\nvfuser_codegen.dll" or one of its dependencies.
Traceback (most recent call last):
  File "<string>", line 1, in <module>
  File "C:\Users\harb\AppData\Local\Programs\Python\Python311\Lib\multiprocessing\spawn.py", line 120, in spawn_main
Traceback (most recent call last):
  File "<string>", line 1, in <module>
  File "C:\Users\harb\AppData\Local\Programs\Python\Python311\Lib\multiprocessing\spawn.py", line 120, in spawn_main
    exitcode = _main(fd, parent_sentinel)
    exitcode = _main(fd, parent_sentinel)
               ^^^^^^^^^^^^^^^^^^^^^^^^^^
               ^^^^^^^^^^^^^^^^^^^^^^^^^^
  File "C:\Users\harb\AppData\Local\Programs\Python\Python311\Lib\multiprocessing\spawn.py", line 130, in _main
  File "C:\Users\harb\AppData\Local\Programs\Python\Python311\Lib\multiprocessing\spawn.py", line 130, in _main
    self = reduction.pickle.load(from_parent)
    self = reduction.pickle.load(from_parent)
           ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
  File "C:\Users\harb\AppData\Local\Programs\Python\Python311\Lib\site-packages\torch\__init__.py", line 1504, in <module>
           ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
  File "C:\Users\harb\AppData\Local\Programs\Python\Python311\Lib\site-packages\torch\__init__.py", line 1442, in <module>
    from . import masked
  File "C:\Users\harb\AppData\Local\Programs\Python\Python311\Lib\site-packages\torch\masked\__init__.py", line 3, in <module>
    import torch.utils.data
  File "C:\Users\harb\AppData\Local\Programs\Python\Python311\Lib\site-packages\torch\utils\data\__init__.py", line 21, in <module>
Traceback (most recent call last):
    from ._ops import (
    from torch.utils.data.datapipes.datapipe import (
  File "<string>", line 1, in <module>
  File "C:\Users\harb\AppData\Local\Programs\Python\Python311\Lib\site-packages\torch\masked\_ops.py", line 11, in <module>
  File "C:\Users\harb\AppData\Local\Programs\Python\Python311\Lib\site-packages\torch\utils\data\datapipes\__init__.py", line 1, in <module>
  File "C:\Users\harb\AppData\Local\Programs\Python\Python311\Lib\multiprocessing\spawn.py", line 120, in spawn_main
    from torch._prims_common import corresponding_real_dtype
    exitcode = _main(fd, parent_sentinel)
    from . import iter
  File "C:\Users\harb\AppData\Local\Programs\Python\Python311\Lib\site-packages\torch\_prims_common\__init__.py", line 23, in <module>
               ^^^^^^^^^^^^^^^^^^^^^^^^^^
  File "C:\Users\harb\AppData\Local\Programs\Python\Python311\Lib\multiprocessing\spawn.py", line 130, in _main
Traceback (most recent call last):
  File "C:\Users\harb\AppData\Local\Programs\Python\Python311\Lib\site-packages\torch\utils\data\datapipes\iter\__init__.py", line 12, in <module>
    import sympy
    self = reduction.pickle.load(from_parent)
  File "<string>", line 1, in <module>
    from torch.utils.data.datapipes.iter.combining import (
  File "C:\Users\harb\AppData\Local\Programs\Python\Python311\Lib\site-packages\sympy\__init__.py", line 108, in <module>
           ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
  File "C:\Users\harb\AppData\Local\Programs\Python\Python311\Lib\multiprocessing\spawn.py", line 120, in spawn_main
Traceback (most recent call last):
  File "<frozen importlib._bootstrap>", line 1178, in _find_and_load
  File "C:\Users\harb\AppData\Local\Programs\Python\Python311\Lib\site-packages\torch\__init__.py", line 1332, in <module>
    from .series import (Order, O, limit, Limit, gruntz, series, approximants,
  File "C:\Users\harb\AppData\Local\Programs\Python\Python311\Lib\site-packages\sympy\series\__init__.py", line 9, in <module>
    exitcode = _main(fd, parent_sentinel)
    from .sequences import SeqPer, SeqFormula, sequence, SeqAdd, SeqMul
    _C._initExtension(manager_path())
MemoryError
               ^^^^^^^^^^^^^^^^^^^^^^^^^^
  File "C:\Users\harb\AppData\Local\Programs\Python\Python311\Lib\multiprocessing\spawn.py", line 130, in _main
    self = reduction.pickle.load(from_parent)
           ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
  File "C:\Users\harb\AppData\Local\Programs\Python\Python311\Lib\site-packages\torch\__init__.py", line 1504, in <module>
    from . import masked
  File "C:\Users\harb\AppData\Local\Programs\Python\Python311\Lib\site-packages\torch\masked\__init__.py", line 3, in <module>
    from ._ops import (
  File "C:\Users\harb\AppData\Local\Programs\Python\Python311\Lib\site-packages\torch\masked\_ops.py", line 11, in <module>
  File "<string>", line 1, in <module>
  File "C:\Users\harb\AppData\Local\Programs\Python\Python311\Lib\multiprocessing\spawn.py", line 120, in spawn_main
    exitcode = _main(fd, parent_sentinel)
               ^^^^^^^^^^^^^^^^^^^^^^^^^^
  File "C:\Users\harb\AppData\Local\Programs\Python\Python311\Lib\multiprocessing\spawn.py", line 130, in _main
    self = reduction.pickle.load(from_parent)
           ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
  File "C:\Users\harb\AppData\Local\Programs\Python\Python311\Lib\site-packages\torch\__init__.py", line 1474, in <module>
    from torch import quantization as quantization
  File "C:\Users\harb\AppData\Local\Programs\Python\Python311\Lib\site-packages\torch\quantization\__init__.py", line 1, in <module>
  File "<frozen importlib._bootstrap>", line 1149, in _find_and_load_unlocked
  File "<frozen importlib._bootstrap>", line 1178, in _find_and_load
    from torch._prims_common import corresponding_real_dtype
    from .quantize import *  # noqa: F403
    ^^^^^^^^^^^^^^^^^^^^^^^
  File "C:\Users\harb\AppData\Local\Programs\Python\Python311\Lib\site-packages\torch\quantization\quantize.py", line 10, in <module>
  File "<frozen importlib._bootstrap>", line 1178, in _find_and_load
  File "<frozen importlib._bootstrap>", line 690, in _load_unlocked
  File "<frozen importlib._bootstrap>", line 1149, in _find_and_load_unlocked
    from torch.ao.quantization.quantize import (
  File "C:\Users\harb\AppData\Local\Programs\Python\Python311\Lib\site-packages\torch\ao\quantization\__init__.py", line 3, in <module>
  File "<frozen importlib._bootstrap_external>", line 936, in exec_module
  File "<frozen importlib._bootstrap_external>", line 1032, in get_code
  File "<frozen importlib._bootstrap_external>", line 1131, in get_data
  File "<frozen importlib._bootstrap>", line 690, in _load_unlocked
  File "<frozen importlib._bootstrap>", line 1149, in _find_and_load_unlocked
    from .fake_quantize import *  # noqa: F403
    ^^^^^^^^^^^^^^^^^^^^^^^^^^^^
  File "C:\Users\harb\AppData\Local\Programs\Python\Python311\Lib\site-packages\torch\ao\quantization\fake_quantize.py", line 8, in <module>
  File "<frozen importlib._bootstrap>", line 690, in _load_unlocked
  File "<frozen importlib._bootstrap_external>", line 936, in exec_module
  File "<frozen importlib._bootstrap_external>", line 936, in exec_module
MemoryError
    from torch.ao.quantization.observer import (
  File "<frozen importlib._bootstrap_external>", line 1032, in get_code
  File "<frozen importlib._bootstrap_external>", line 1131, in get_data
MemoryError
  File "<frozen importlib._bootstrap_external>", line 1032, in get_code
  File "<frozen importlib._bootstrap_external>", line 1131, in get_data
MemoryError
  File "C:\Users\harb\AppData\Local\Programs\Python\Python311\Lib\site-packages\torch\ao\quantization\observer.py", line 15, in <module>
    from torch.ao.quantization.utils import (
  File "C:\Users\harb\AppData\Local\Programs\Python\Python311\Lib\site-packages\torch\ao\quantization\utils.py", line 12, in <module>
    from torch.fx import Node
  File "C:\Users\harb\AppData\Local\Programs\Python\Python311\Lib\site-packages\torch\fx\__init__.py", line 83, in <module>
    from .graph_module import GraphModule
  File "C:\Users\harb\AppData\Local\Programs\Python\Python311\Lib\site-packages\torch\fx\graph_module.py", line 8, in <module>
    from .graph import Graph, _PyTreeCodeGen, _is_from_torch, _custom_builtins, PythonCode
  File "C:\Users\harb\AppData\Local\Programs\Python\Python311\Lib\site-packages\torch\fx\graph.py", line 2, in <module>
    from .node import Node, Argument, Target, map_arg, _type_repr, _get_qualified_name
  File "C:\Users\harb\AppData\Local\Programs\Python\Python311\Lib\site-packages\torch\fx\node.py", line 36, in <module>
    _ops.aten._assert_async.msg,
    ^^^^^^^^^^^^^^^^^^^^^^^
  File "C:\Users\harb\AppData\Local\Programs\Python\Python311\Lib\site-packages\torch\_ops.py", line 757, in __getattr__    op, overload_names = torch._C._jit_get_operation(qualified_op_name)
                         ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
MemoryError: bad allocation
Plotting labels to runs\detect\wf9\labels.jpg...
optimizer: 'optimizer=auto' found, ignoring 'lr0=0.01' and 'momentum=0.937' and determining best 'optimizer', 'lr0' and 'momentum' automatically...
optimizer: AdamW(lr=0.002, momentum=0.9) with parameter groups 57 weight(decay=0.0), 64 weight(decay=0.0005), 63 bias(decay=0.0)
Image sizes 640 train, 640 val
Using 8 dataloader workers
Logging results to runs\detect\wf9
Starting training for 100 epochs...

      Epoch    GPU_mem   box_loss   cls_loss   dfl_loss  Instances       Size
  0%|          | 0/2 [00:05<?, ?it/s]
Traceback (most recent call last):
  File "C:\Users\harb\AppData\Local\Programs\Python\Python311\Lib\site-packages\torch\utils\data\dataloader.py", line 1132, in _try_get_data
    data = self._data_queue.get(timeout=timeout)
           ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
  File "C:\Users\harb\AppData\Local\Programs\Python\Python311\Lib\queue.py", line 179, in get
    raise Empty
_queue.Empty

The above exception was the direct cause of the following exception:

Traceback (most recent call last):
  File "<frozen runpy>", line 198, in _run_module_as_main
  File "<frozen runpy>", line 88, in _run_code
  File "C:\Users\harb\AppData\Local\Programs\Python\Python311\Scripts\yolo.exe\__main__.py", line 7, in <module>
  File "C:\Users\harb\AppData\Local\Programs\Python\Python311\Lib\site-packages\ultralytics\cfg\__init__.py", line 448, in entrypoint
    getattr(model, mode)(**overrides)  # default args from model
    ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
  File "C:\Users\harb\AppData\Local\Programs\Python\Python311\Lib\site-packages\ultralytics\engine\model.py", line 338, in train
    self.trainer.train()
  File "C:\Users\harb\AppData\Local\Programs\Python\Python311\Lib\site-packages\ultralytics\engine\trainer.py", line 190, in train
    self._do_train(world_size)
  File "C:\Users\harb\AppData\Local\Programs\Python\Python311\Lib\site-packages\ultralytics\engine\trainer.py", line 320, in _do_train
    for i, batch in pbar:
  File "C:\Users\harb\AppData\Local\Programs\Python\Python311\Lib\site-packages\tqdm\std.py", line 1182, in __iter__
    for obj in iterable:
  File "C:\Users\harb\AppData\Local\Programs\Python\Python311\Lib\site-packages\ultralytics\data\build.py", line 42, in __iter__
    yield next(self.iterator)
          ^^^^^^^^^^^^^^^^^^^
  File "C:\Users\harb\AppData\Local\Programs\Python\Python311\Lib\site-packages\torch\utils\data\dataloader.py", line 630, in __next__
    data = self._next_data()
           ^^^^^^^^^^^^^^^^^
  File "C:\Users\harb\AppData\Local\Programs\Python\Python311\Lib\site-packages\torch\utils\data\dataloader.py", line 1328, in _next_data
    idx, data = self._get_data()
                ^^^^^^^^^^^^^^^^
  File "C:\Users\harb\AppData\Local\Programs\Python\Python311\Lib\site-packages\torch\utils\data\dataloader.py", line 1284, in _get_data
    success, data = self._try_get_data()
                    ^^^^^^^^^^^^^^^^^^^^
  File "C:\Users\harb\AppData\Local\Programs\Python\Python311\Lib\site-packages\torch\utils\data\dataloader.py", line 1145, in _try_get_data
    raise RuntimeError(f'DataLoader worker (pid(s) {pids_str}) exited unexpectedly') from e
RuntimeError: DataLoader worker (pid(s) 8948, 8988, 7844, 8940) exited unexpectedly

C:\Users\harb\Downloads\TrainingFolder>python
Python 3.11.0 (main, Oct 24 2022, 18:26:48) [MSC v.1933 64 bit (AMD64)] on win32
Type "help", "copyright", "credits" or "license" for more information.
>>> import torch
>>> torch.cuda.is_available()
True
>>>
