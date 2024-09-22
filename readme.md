New app rewritten using monitor's hkx2e library as a basis. Currently built solely as an MCO to BFCO converter handling both file/pathname conversion and annotation patching
Automatically backs up everything and keeps a log (which might crash your PC if you open it due to being way to verbose). should be safe to use but it is not thoroughly tested and hasnt been tested at all ingame. Probably only compatible with animations already converted to 64 bit though I could be wrong. Doesn't require havok sdk or any prereqs except dotnet 8.0. Should work with the base hk2xe library by literally just dropping this file in and building

Usage: run the exe with launch arg to your desired directory in quotes or launch it and type your directory without quotes (will fix later idk why c# is so inconsistent). You can use it on any directory with animations in it. It can be used on your entire mod library but it will probably crash due to specific unhandled errors so be aware. It can also be used on a single mod at a time and will prompt to go again. Doesnt actually have a way to convert to xml yet so use hkxconv if you want to verify results without checking log. Acts only on files with mco or bfco in the name or parent name if preceded by _variant_ 

So lots of stipulations, maybe it doesnt sound that appealing yet but I havent mentioned the speed. This sucker is astoundingly fast. If you run it on a small set of mods it will be done near instantly. On a full mod library it will take some time but its easily hundreds of times faster than the python based version I tested earlier this year and infinitely faster than the public mco-bfco util since it doesnt support batch processing at all

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

level 1.0 (Current):

simple python cli, still relies on hkxcmd + hkxconv but handles all external calls in native python as subprocess calls

dump, replace sequentially (edit specific members of dump), insert, replace by json map, import as py module for use with multithreaded GUI for mass preconfigured changes  


1.x: 

batching added to cli, standalone GUI, cythonize if possible


level 2.0: 

dotnet module importing hkx2 and xmlcake to replace external requirements with a dll library providing ctype exports

level 3.0 (started this already, but will go back and do level 2.0 first):

bye bye python, full dotnet GUI integration standalone or as a configured mode for pandora 
