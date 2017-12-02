# WhiteCow
- In Mooh we trust
- I like to mooh
- Let's mooh it

## disclaimer
You use this tool at your own risk, You are the only one responsible for looses.

# Process
to complete
## Supported Broker for trading :
- BitFinex
- Poloniex
## Installation
### Pre-requisite
### IT
- mono-complete

### Broker
- For Poloniex you need to have an account with an api that have enable trading **and withdraw**
- For Bitfinex you need to have an account with the [greenlane](https://www.bitfinex.com/withdraw/greenlane) enable
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

### update configuration settings
modify the file WhiteCow.exe.config

