## v0.9.5
日期：2026-04-18

### Fixes
- 将默认thread num从100%cpu占有改成默认70%的cpu逻辑核数，以避免在UI的场合卡顿;

### Feature
- 更新nuget包从0.9.4升级到0.9.5;

### Document
- 优化文档，加上自动化场景下参数对坐标影响的注意事宜;
- 考虑国际影响，改成首页默认英文为主;

## v0.9.4
日期：2026-04-15

### Feature

- 提供模型初始化及Rection的各个async/await的异步版本;
- 提供nuget包:版本0.9.4

### Document
- 完善了中英文文档

## v0.9.3
日期：2026-04-14

### Feature

- 提供模型初始化及Rection的各个async/await的异步版本;
- 提供nuget包，版本0.9.3,以方便在线下载;


## v0.9.2
日期：2026-04-13

### Fixes

> 升级各个依赖包
- 将Clipper升级到Clipper2;
- 升级Emugu.CV、OnnxRuntime等，引入依赖Emugu.CV.Bitmap;

### Feature

- 将SDK从特定版本改成netstand,以同时支持.net Framework及.net最新的各个版本;
