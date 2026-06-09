# 新手拉取项目指南

这份说明写给第一次用 GitHub 的人。目标是把项目从 GitHub 下载到自己的电脑，并用 Unity 打开。

项目地址：

```text
https://github.com/mm007711/unauthorized-passenger.git
```

## 需要准备

先安装这两个软件：

- Unity Hub
- GitHub Desktop

Unity 版本建议使用项目当前版本：

```text
Unity 2022.3.62f2
```

如果 Unity Hub 提示版本不完全一致，优先安装 `2022.3.62f2`。如果暂时没有这个版本，也可以先用相近的 `2022.3 LTS` 打开，但第一次打开时可能会有升级或重新导入提示。

## 推荐方式：用 GitHub Desktop 拉取

1. 打开 GitHub Desktop。
2. 点击顶部菜单 `File`。
3. 点击 `Clone repository...`。
4. 切到 `URL` 选项。
5. 在 `Repository URL` 填入：

```text
https://github.com/mm007711/unauthorized-passenger.git
```

6. 在 `Local path` 选择你想保存项目的位置。

推荐类似这样：

```text
D:\UnityProjects\unauthorized-passenger
```

7. 点击 `Clone`。
8. 等下载完成。

下载完成后，本地文件夹里应该能看到这些目录：

```text
Assets
Packages
ProjectSettings
```

看到这三个目录，基本就说明项目拉取对了。

## 用 Unity Hub 打开项目

1. 打开 Unity Hub。
2. 点击 `Add` 或 `Add project from disk`。
3. 选择刚才下载的项目文件夹。

注意：选择的是包含 `Assets`、`Packages`、`ProjectSettings` 的那一层文件夹，不是里面的 `Assets` 文件夹。

4. Unity Hub 会把项目加到列表里。
5. 点击项目打开。
6. 第一次打开会比较慢，Unity 会重新生成 `Library` 缓存，这是正常的。

## 打开后怎么运行

1. 在 Unity 顶部打开任意场景，默认可以用：

```text
Assets/Scenes/SampleScene.unity
```

2. 点击 Unity 顶部中间的播放按钮。
3. 运行后会自动出现 GAL 模板标题页。

## 如果只是想下载一份，不需要同步更新

也可以不用 GitHub Desktop，直接下载 ZIP：

1. 打开网页：

```text
https://github.com/mm007711/unauthorized-passenger
```

2. 点击绿色 `Code` 按钮。
3. 点击 `Download ZIP`。
4. 解压 ZIP。
5. 用 Unity Hub 添加解压后的项目文件夹。

这种方式适合只看一次项目；如果之后还要接收更新，建议用 GitHub Desktop。

## 之后如何更新项目

如果是用 GitHub Desktop 克隆的项目：

1. 打开 GitHub Desktop。
2. 左上角确认当前仓库是 `unauthorized-passenger`。
3. 点击 `Fetch origin`。
4. 如果按钮变成 `Pull origin`，再点一次。
5. 等它完成后，再打开 Unity。

更新前最好先关闭 Unity，避免 Unity 正在占用文件。

## 常见问题

### Unity 打开很慢

第一次打开会生成本地缓存，几分钟是正常的。以后会快很多。

### Unity Hub 提示版本不一致

优先安装 `Unity 2022.3.62f2`。如果只是预览，也可以用同系列 `2022.3 LTS` 打开。

### 打开后看不到游戏画面

确认已经点击 Play。这个模板运行时会自动生成 UI，不需要手动拖预制体。

### GitHub Desktop 下载失败

先检查网络，再重新点 `Clone`。如果一直失败，可以临时用 `Download ZIP`。

### 不小心选错了文件夹

Unity Hub 添加项目时，要选包含下面三个目录的文件夹：

```text
Assets
Packages
ProjectSettings
```

不要直接选择 `Assets` 文件夹。

## 给协作者的简单流程

日常只需要记住：

```text
打开 GitHub Desktop -> Fetch origin -> Pull origin -> 打开 Unity Hub -> 打开项目 -> Play
```

如果要改文案，优先改这些 CSV：

```text
Assets/StreamingAssets/GAL/Text/story_text_zh-CN.csv
Assets/StreamingAssets/GAL/Text/story_text_en.csv
Assets/StreamingAssets/GAL/Text/story_text_ja.csv
```

如果要替换立绘，把图片放到：

```text
Assets/StreamingAssets/GAL/Portraits/<角色ID>/<差分名>.png
```

例如：

```text
Assets/StreamingAssets/GAL/Portraits/test/neutral.png
```
