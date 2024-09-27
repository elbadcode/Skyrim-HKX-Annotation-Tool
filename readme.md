New app rewritten using monitor's hkx2e library as a basis. Currently built solely as an MCO to BFCO converter handling both file/pathname conversion and annotation patching

Automatically backs up everything and keeps a log (which might crash your PC if you open it due to being way too verbose). should be safe to use and fully works ingame although some adjustments need to made to utilize all of BFCO's features. Likely needs animations already converted to 64 bit though I haven't tested. Doesn't require havok sdk or any prereqs except dotnet 8.0. Should work with the base hk2xe library by literally just dropping this file in and building

Usage: run the exe with launch arg to your desired directory in quotes or launch it and type your path or right click the location in explorer and use copy as path (cli handles quotes and escapes automatically). You can use it on any directory with animations in it including with recursion (please don't go too deep, I didn't handle excessive recursion yet). It can be used on your entire mod library but it will probably crash due to specific unhandled errors so be aware. It can also be used on a single mod at a time and will prompt to go again. Doesnt actually have a way to convert to xml yet so use hkxconv if you want to verify results without checking log. Acts only on files with mco or bfco in the name or parent name if preceded by _variant_ 

So lots of stipulations, maybe it doesnt sound that appealing yet but I havent mentioned the speed. This sucker is astoundingly fast. If you run it on a small set of mods it will be done near instantly. On a full mod library it will take some time but its easily hundreds of times faster than the python based version I tested earlier this year and infinitely faster than the public mco-bfco util since that one doesnt support batch processing at all

Future updates will include full on user chosen annotation replacement and other various nice features. I'll probably rewrite/port in my other skyrim python based utilities I wrote earlier this year for OAR



this is a standalone build of my hkanno replacement I am writing as part of a larger project for animation file management

reason: hkanno is slow, hard to use in a batch way, and requires a specific hct setup


hkanno replacement iceberg:

level 0:
batch scripts for mass conversion and recursive python scripts execution

```
for /r %%* in (*.hkx) do (
hkxcmd.exe convert -v:amd64 "%%~*%"
)
```

```
@echo off
for /r %%* in (*.hkx) do (
cd /D "%%~dp*"
hkxconv.exe convert "%%~*%"
 "%%~*%"
cd /D "%~dp0"
)
```

````
@echo off
for /r %%* in (*.hkx) do (
python annotation_edit.py "%~dp1%%~*%" 
cd /D "%~dp1"
)
````

level 1.0:

simple python cli, still relies on hkxcmd + hkxconv but handles all external calls in python as subprocess calls

dump, replace sequentially (edit specific members of dump), insert, replace by json map, import as py module for use with multithreaded GUI for mass preconfigured changes  


1.x: 

batching added to cli, standalone GUI, acceleration with nuitka compiler. still bottlenecked by hkconv


level 2.0 (Current):

dotnet app using hkx2e to replace external requirements. preconfigured mco to bfco conversion. needs 1 small change before being posted on nexus (add NextIsAttack/PowerAttack annotation for opposing condition and set time for both NextIs annotations to 0.0)

level 3.0:

GUI with all my scripts for OAR management rewritten in dotnet. user configurable annotation remapping and addition


