### 日志上报工具，为了方便测试人员上报问题，开发人员快速定位问题，开发的一键上报日志工具。当开启该功能后，会记录日志到本地，然后压缩日志文件为zip，发送到指定邮箱

### 使用流程
#### 1. 发送邮箱开启SMTP服务，获取授权码
#### 2. 初始化配置
```c#
LRConfig config = new LRConfig("server", "port", "email", "code", "title", new string[]{"toEmail1","toEmail2"});
config.btnColor = Color.red;
config.longPressColor = Color.green;
LogReport.Init(config);
```

### 操作流程
#### 1. 开启功能，开始记录日志
#### 2. 点击悬浮按钮，出现输入框，用来标记该Log的问题，不输入是不能发送的
#### 3. 再次点击悬浮按钮，发送邮件
#### 4. 长按悬浮按钮，可拖拽，避免遮挡游戏内容

### 注意：LRConfig参数不能通过明文传递，需要经过base64编码后传递