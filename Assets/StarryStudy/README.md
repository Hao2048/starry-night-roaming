# 星空漫游

使用、操作、设计思路和展示视频见仓库根目录 [README](../../README.md)。

唯一运行场景：`StarryNightPreview.unity`。运行时场景由真实三维短笔触网格组成；原画图像仅用于封面和入场过渡。

- `StarryStudyController.cs`：移动、观察、初始机位和运镜状态。
- `StarryCinematicTour.cs`：镜头路径采样。
- `StarryPaintingTransition.cs`：原画到实时场景的交接。
- `UI/`：封面、暂停、操作指南和字体。
- `Editor/ReferenceStrokeReconstruction.cs`：离线拟合笔触。
- `Editor/PainterlyTownBuilder.cs`：城镇、道路、麦田、山体和地面。
- `Shaders/MeasuredPigment.shader`：顶点色与连续小幅流动，无噪点溶解。

已生成网格随仓库提交，无需重建即可进入场景。编辑器菜单中的早期美术实验工具仅用于开发，不是首次打开工程的必需步骤。重建前请提交修改；新增网格需要同步更新根目录 `.gitignore` 的场景资源白名单。

素材许可见根目录 [THIRD_PARTY_NOTICES](../../THIRD_PARTY_NOTICES.md)。
