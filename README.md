# VSGUI

![](https://github.com/YohoYang/VSGUI/raw/master/READMEIMG/VSGUI-ICON.png)

[ENIGLISH README](https://github.com/YohoYang/VSGUI/blob/master/README-en.md)

一款全新VapourSynth视频压制软件。
A video encode GUI like MeGUI for VapourSynth.

### 下载地址

[软件主体](https://github.com/YohoYang/VSGUI/releases)

[.NET 6 运行库](https://aka.ms/dotnet/6.0/windowsdesktop-runtime-win-x64.exe)

### 主要特性

- 基于Vapoursynth（处理视频）和Avisynth（处理音频）；
- 集成所需所有环境，开箱即用，不需要安装配置环境；
- 支持简易压制，拖入视频自动生成vpy脚本并压制视频和音频，并自动封装（支持自动识别简单的反交错等）；
- 支持高级压制，手动书写Vapoursynth脚本，并分别处理视频和音频流压制；支持批量添加任务；
- 支持自定义编码器，理论上支持所有编码器编码；
- 集成[VSREPO GUI](https://github.com/theChaosCoder/VSRepoGUI "VSREPO GUI")，可以简便的选择并下载自己所需的滤镜库；
- 集成[VS Editor](https://github.com/YomikoR/VapourSynth-Editor "VS Editor")，用以编辑和预览vpy脚本；
- 支持音频裁剪，支持对VFR音频进行同步；
- 使用eac3to和ffmpeg等对视频进行解流；
- 一键进行简易封装；
- 软件自动更新；
- 便于字幕组统一管理的压制参数订阅；
- 多语言支持（目前支持简体中文，English）；

### 特别说明

- 本软件从某种程度上来说，并不是设计给小白使用，需要有一定的压制基础，目的是用于平替MEGUI的对应功能，并迁移至vapoursynth；
- 由于vapoursynth存在多种API版本，本软件仅支持v4及以上版本api使用

### 软件截图

![中文简易压制主界面](https://github.com/YohoYang/VSGUI/raw/master/READMEIMG/1.png)
![英文高级压制主界面](https://github.com/YohoYang/VSGUI/raw/master/READMEIMG/2.png)
