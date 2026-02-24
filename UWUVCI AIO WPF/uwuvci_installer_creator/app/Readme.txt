============================================================
                   UWUVCI v3 ReadMe (By ZestyTS)
============================================================

Thank you for downloading UWUVCI-V3!
If you didn’t download it from the official GitHub source, you might be using a modified version.

Official source:
  https://github.com/stuff-by-3-random-dudes/UWUVCI-AIO-WPF/releases

If you're looking for the FAQ, keep scrolling.
If you're curious about the Discord, the latest updates, or video guides, you’ll find everything below.

============================================================
 Community & Resources
============================================================
• Discord:
  https://discord.gg/mPZpqJJVmZ

• Latest changes:
  https://github.com/stuff-by-3-random-dudes/UWUVCI-AIO-WPF/releases/latest

• Official UWUVCI V3 Video Series:
  https://www.youtube.com/watch?v=ADde8cZ6-y8

============================================================
 Project Overview
============================================================
By the time you're reading this, **active development on UWUVCI-V3 has concluded.** As in there are no planned updates remaining. (Although, there have been seven updates since developent ended.)
Future development continues under UWUVCI-Prime (v4), which adds full Mac and Linux support.

I’m **ZestyTS**, and since late 2020 I’ve been the primary developer maintaining and improving UWUVCI-V3.
My goal was to fix legacy bugs, modernize the code, and make the program stable, fast, and cross-platform friendly.

============================================================
 Major Features Introduced in UWUVCI v3
============================================================
These are the major features and improvements I personally implemented:

• Widescreen support for N64
• DarkFilte removal for N64 and GBA
• C2W overclock patching for Wii injects
• GCT patching and VFilter (deflicker) options for Wii
• Full support for Windows 7 and 8
• Support for Unix (Wine/macOS/Linux)
• Automatic dependency detection and installation
• Rewritten Installer with guided setup and OneDrive-safe paths
• “First-Run Tutorial” wizard
• A ReadMe/Patch Notes Viewer
• Logging system (auto-clears after 7 days)
• Async refactor, faster inject creation and UI responsiveness
• CNUSPACKER and WiiUDownloader rewritten as DLLs
• Added tooltips, better error handling, and smoother UI behavior
* Compat/Images/Feedback updates from the app itself
* NDS Configuration settings
• Updated to .NET Framework 4.8 and C# 13

In short, UWUVCI v3 became more self-contained, faster, and significantly more stable.

============================================================
 Support the Developers
============================================================
❤️ Donate to me (ZestyTS):  
  https://ko-fi.com/zestyts

💚 Donate to the original creator (NicoAICP):  
  https://ko-fi.com/uwuvci

============================================================
 Frequently Asked Questions (FAQ)
============================================================
Maintained by ZestyTS (2020–2025)  
This FAQ was rewritten during v3.200 after major system updates.
Please read carefully before assuming something is broken.

============================================================
 🔰 Getting Started
============================================================

Q1) I don’t know how to use UWUVCI.  
A) Go here:  
   https://uwuvci-prime.github.io/UWUVCI-Resources/index  
   Select your console and follow the steps exactly.  
   Don’t skip steps or use random YouTube guides (UWUVCI has it's own video guides).

------------------------------------------------------------

Q2) What games are compatible?  
A) Visit: https://uwuvci.net → click “Compatibility” (top right).  
   If a game isn’t listed, it’s **untested**, not unsupported.  
   For GameCube: Rhythm Heaven Fever as a base works for all titles.

------------------------------------------------------------

Q3) What does “Base” mean in the dropdown?  
A) The base game is the template UWUVCI uses to inject your selected title.

------------------------------------------------------------

Q4) “Base not downloaded”?  
A) The base game is missing.  
   Fix: Click “Enter TKey” and input your Title Key for the purchased base.

------------------------------------------------------------

Q5) How do I get the Title Key?  
A) Buy the base from the eShop → dump using Tik2SD.  
   ⚠️ Title Key sharing = piracy. Don’t do that.

------------------------------------------------------------

Q6) What’s the Common Key?  
A) The Wii U system decryption key.  
   • Have a NAND backup? Use `otp.bin`.  
   • Otherwise, follow: https://wiiu.hacks.guide/aroma/nand-backup.html

------------------------------------------------------------

Q7) Base download stuck or slow?  
A) Nintendo’s servers can lag. Try again later.  
   • Normal injects: under 5 minutes  
   • Large games (e.g. Xenoblade): longer (~8 GB)

============================================================
 ⚙️ Setup & General Issues
============================================================

Q8) Antivirus flagged UWUVCI?  
A) False positive, whitelist it. Nothing malicious is inside.

------------------------------------------------------------

Q9) “Could not find file 'bin\\temp\\pre.iso'”?  
A) Bad game dump, redump.

------------------------------------------------------------

Q10) “Path .../temp/temp missing” or “tmd.bin can’t be found”?  
A) Same issue, invalid dump. Redump properly.

------------------------------------------------------------

Q11) UWUVCI doesn’t open.  
A) Install .NET Framework 4.8:  
   https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48
   Still not opening? See Q29.

------------------------------------------------------------

Q12) UWUVCI says “Drive is full (12 GB)”.  
A) Move UWUVCI to a drive with more free space.

------------------------------------------------------------

Q13) UWUVCI crashes, UI disappears, or acts strange.  
A) Check that:  
   • You didn’t install or using a rom in OneDrive / cloud folder  
   • Antivirus isn’t blocking background tools  
   • Turkish locale has known to cause issues

============================================================
 💾 Injection & Compatibility
============================================================

Q14) Inject created but game doesn’t launch properly.  
A) Check all of these:  
   • Correct base game (region-matched)  
   • Base ≥ target game size (GCN/Wii not applicable)
   • Use unmodified ROMs  

------------------------------------------------------------

Q15) My game doesn’t appear when I select “ROM Path.”  
A) The file is in an unsupported format.
   When a rom is being selected, the pop-up box will specify what file types are supported for the console.

------------------------------------------------------------

Q16) WUP install fails / Error 199-9999.  
A) Missing sigpatches.  
   Download:  
   https://github.com/V10lator/SigpatchesModuleWiiU/releases/download/v1.0/01_sigpatches.rpx  
   Place in:  
   sd:/wiiu/environments/aroma/modules/setup

------------------------------------------------------------

Q17) GCN/Wii injects not working.  
A) Usually SDUSB or ISFShax homebrew issues or plugin issues with WiiVC Launch.  
   Also verify your Nintendont setup (see Q17/18).

------------------------------------------------------------

Q18) GCN inject boots to the Nintendont menu.  
A) You used TeconMoon injector before.  
   Fix:  
   • Delete `nincfg.bin` from SD root  
   • Delete `apps/nintendont` folder  
   • Re-run “SD Setup” in UWUVCI

------------------------------------------------------------

Q19) “boot.dol not found”.  
A) Nintendont not set up on SD. Run “SD Setup” again.

------------------------------------------------------------

Q20) SaveMii can’t find my injects.  
A) Use **SaveMii Inject MOD**, not the vanilla version.

------------------------------------------------------------

Q21) GB/C games don’t save when using the VC reset button.  
A) Normal behavior, GoombaColor doesn’t handle VC resets.  
   Use the in-game reset button combo instead.

------------------------------------------------------------

Q22) “NKit error?”  
A) You used a pirated or modified dump. Use a real ISO.  
   UWUVCI does **not** support illegal or altered files.

------------------------------------------------------------

Q23) “Stuck on ‘Copying to SD’”.  
A) Manually copy it yourself:  
   Go to UWUVCI’s `InjectedGames` folder → move the inject to your SD card.

------------------------------------------------------------

Q24) Help with ROM hacks or mods?  
A) Mods are unsupported.  
   If it runs on real hardware, it might work here, but ask the mod’s community.  
   UWUVCI can’t guarantee mod compatibility.

============================================================
 🧰 Advanced Troubleshooting
============================================================

Q25) “UWUVCI still won’t open” after installing .NET.  
A) Check Windows Event Viewer for crash details.  
   If it references missing DLLs, rerun the installer.

------------------------------------------------------------

Q26) “Could not load CNUSPACKER.dll” or similar.  
A) Required DLLs are missing, rerun the installer to restore them.

------------------------------------------------------------

Q27) UWUVCI’s progress bar gets stuck.  
A) Check out the Logs, they write everything out.
   ⚙️ → “App Settings” -> “Open Log Folder”
   Click on the most recent file.

------------------------------------------------------------

Q28) Mac/Linux version?  
A) UWUVCI-V3 uses WPF, a Windows-only framework.  
   Use Wine or CrossOver, UWUVCI auto-detects non-Windows systems.  
   UWUVCI-Prime (v4) will be natively cross-platform.

------------------------------------------------------------

Q29) Where are the Log and Settings files?  
A) Windows:  
      %localappdata%\UWUVCI-V3  
   Mac/Linux (Wine):  
      ~/.wine/drive_c/users/$USER/AppData/Local/UWUVCI-V3

------------------------------------------------------------

Q30) Image/Feedback/Compatibility Submission Failed  
A) If the error is stating something about a **Token**
      That means the credentials have expired.
      Credentials will only be expired when a new version of UWUVCI V3 is released.
   If the error is stating **Access Restricted*
      You've been flagged as a bad actor from your submissions. 

------------------------------------------------------------

Q31) “An error message popped up.”  
A) **Read it.**  
   UWUVCI’s messages are written to tell you exactly what’s wrong.  
   If it mentions a file, check that path.  
   If it says “missing dependency,” rerun the installer.  
   If it says “drive full,” free up space.  
   It’s not random, it’s there to help you.


============================================================
 🪟 Windows-Only + Startup Troubleshooting
============================================================

Q32) Is UWUVCI-V3 Windows-only?
A) Yes.
   UWUVCI-V3 is built on WPF (.NET Framework), which is Windows-only.
   Mac/Linux can run it only through Wine-like compatibility layers.

------------------------------------------------------------

Q33) User clicked through tutorial, app closes, and no log is created.
A) This usually means first-run settings/log path could not be created.
   Ask the user to follow these steps in order:

   1) Fully extract UWUVCI first.
      Do NOT run it from inside a ZIP.

   2) Move UWUVCI out of protected folders.
      Recommended: C:\UWUVCI\
      Avoid: C:\Program Files\ and OneDrive-synced folders.

   3) Launch UWUVCI once as Administrator.

   4) Verify these folders exist and are writable:
      %LOCALAPPDATA%\UWUVCI-V3\
      %LOCALAPPDATA%\UWUVCI-V3\Logs\

   5) If using Windows Defender Controlled Folder Access:
      Add UWUVCI AIO.exe as an allowed app.

   6) Reinstall .NET Framework 4.8 and reboot:
      https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48

   7) Try again, then send the newest file from:
      %LOCALAPPDATA%\UWUVCI-V3\Logs\

------------------------------------------------------------

Q34) Still no logs after all steps?
A) Collect Event Viewer entries:
   Windows Logs -> Application
   Filter by source: ".NET Runtime" and "Application Error"
   Send the latest error details with timestamp.
============================================================
 📺 Extra Resources
============================================================
Official Video Guide:  
  https://www.youtube.com/watch?v=I5UdYcVSRSA&list=PLbQMtrmXFIxQ1hpvu9m1th41vsaqnZ2Id  

Discord Support:  
  https://discord.gg/mPZpqJJVmZ

============================================================
 End of ReadMe
============================================================
Maintained by ZestyTS, UWUVCI V3

