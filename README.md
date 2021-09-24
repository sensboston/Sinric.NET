# Sinric.NET

forked from https://github.com/odaruc/sinric-pro-csharp but refactored from netstandard 2.1/netcore 3.1 to .NET v 4.7.2

Also, working app release built from this repo (netcore standalone executable has a size about 60Mb! Compare with current release app size, it's just 380Kb)

**How to use released app gh4pc (Google Home For PC)**

_(instructions provided for Google Home users only, I don't use Alexa or any other smart home system)_

- Open [SinricPro website](https://sinric.pro/index.html) and create **free** account
- Create a new device of type "Thermostart", name it "My PC" (or whatever you want), assign to correct room (where is your media PC is located)
- Open "Google Home" app on your phone
- Link "SinricPro" service by adding new device
- Your recently created **"My PC"** device should appear immediatelly in the selected room (at Google Home app), and it will be... a brand new "thermostate" (surprise!) 😉 
- Now, tap on Google Home "Routines" and add a few new routines to control your PC. For the instance, I created:

   * **Starter** -> "_When I say to my Assistant_" voice command: "Play video", **Action** -> _Adjust thermostat_ to temperature 10°
   * **Starter** -> "_When I say to my Assistant_" voice command: "Pause", **Action** -> _Adjust thermostat_ to temperature 11°
   * **Starter** -> "_When I say to my Assistant_" voice command: "Mute video", **Action** -> _Adjust thermostat_ to temperature 12°
   * **Starter** -> "_When I say to my Assistant_" voice command: "Volume up", **Action** -> _Adjust thermostat_ to temperature 13°
   * **Starter** -> "_When I say to my Assistant_" voice command: "Volume down", **Action** -> _Adjust thermostat_ to temperature 14°
   * **Starter** -> "_When I say to my Assistant_" voice command: "Full screen", **Action** -> _Adjust thermostat_ to temperature 15°
  
    (so, I hope you've got my idea: I'm using exact temperature value as a unique command identifier)
    
- Now, download and store on your PC [released app](https://github.com/sensboston/Sinric.NET/releases/download/Rel_1.0.0/gh4pc.exe) from this repo
- Create shortcut to this app on your desktop, open to edit, and add startup parameters to the **Target** field (**this is an important!**), something like
```
D:\Sinric.NET\gh4pc\bin\Release\gh4pc.exe APP_KEY:27c1a817-xxxx-xxxx-xxxx-97af634bcc28 APP_SECRET:faa99618-xxxx-xxxx-be07-e1d24d70bf50-b1848b51-xxxx-xxxx-xxxx-316ce4acffe8 DEVICE_ID:614cfxxxxx113073xxxxx
```
where are **APP_KEY, APP_SECRET, DEVICE_ID** your real values from the SinricPro account/dashboard
  - save shortcut and move it (if you want of course) to the PC's "Startup" folder (to automatically run each time you've logged to the Windows)

**Yopu should be allset!**

_Please note: this app don't have any UI, even visible window, it runs like a service, hidden. To kill it, open Task Manager, go to "Details" tab, find "gh4pc.exe" app and kill it._
