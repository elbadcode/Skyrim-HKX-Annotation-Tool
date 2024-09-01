I have a prototype in hkx2e library in testing just lots going on 


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
