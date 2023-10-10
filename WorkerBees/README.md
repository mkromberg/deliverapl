# AplWorkerBees
A small example template solution to run multiple hosted APL sessions in separate threads inside a single process

## How to get started
1. Clone the repository with "git clone https://github.com/mkromberg/deliverapl/".
2. The WorkerBees solution is in /deliverapl/WorkerBees
3. Copy these Dyalog binaries to src\Dyalog (they are needed for the solution to compile):
```
bridge182-64_unicode.dll
dyalog182_64rt_unicode.dll
dyalog182_64_unicode.dll
dyalogc64_unicode.exe
dyalognet.dll
```
   You can also use the batch file src\CopyDyalogBin, e.g. CopyDyalogBin "C:\Program Files\Dyalog\Dyalog APL-64 18.2 Unicode".

4. Compile the solution with Visual Studio. Then you should hopefully see AplClasses.dll and WorkerBeesManager.dll (+ some Dyalog binaries) in src\bin\Debug (or Release) 

> NB! Has only been tested with 18.2 for now!

## Try it out
Load AplWorkerBees.dws Dyalog APL workspace in Dyalog APL.
First you will have to tell where the newly compiled dll's are placed:

`#.libDLL←'<path to \src\bin\Debug\>' (e.g. #.libDLL←'C:\Repos\AplWorkerBees\src\bin\Debug\')`

Then run e.g. following expressions:
```
bees←GetBees 0
bees.FixFunction ⊂⊂⎕NR 'queens'
tasks←bees.CallWithResultAsync ⊂'queens' 12
⍴¨tasks.Result
⍴¨queens IIÏ (⊃⍴bees)⍴12
```