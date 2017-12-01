# WhiteCow
- In Mooh we trust
- I like to mooh
- Let's mooh it

## Installation
### Pre-requisite
- monocompile

## Installation For Raspberry
```
git clone https://github.com/ronki2304/WhiteCow.git
cd WhiteCow
msbuild  WhiteCow.sln  /p:Configuration=Release
```

the binary file are located in WhiteCow/bin/Release


### Create systemD unit
nano /lib/systemd/system/WhiteCow.service

copy paste the following content :
```
[Unit]
Description=Test service
After=network.target

[Service]
Type=simple
ExecStart=/usr/bin/mono-service {YourPath}/WhiteCow.exe -l:/tmp/WhiteCow.lock --debug
WorkingDirectory={YourPath}             
RestartSec=10
Restart=on-failure

[Install]
WantedBy=multi-user.target
```
