## Intro
The purpose of this project is to create convenient sandbox for different texture synthesis algorithms.  
Algorithms implementations are based on mxgmn repo https://github.com/mxgmn/TextureSynthesis  
Project created for Microsoft Visual Studio 2019 and designed to use on Windows platform. 
Using SynTex you can create spreadsheets like these:  

Full neigbourhood search basic tests: [ExperimentsFNS.md](ExperimentsFNS.md)  
Coherent neighborhood search (K=3) basic tests: [ExperimentsCOH3.md](ExperimentsCOH3.md)  

...  

## Usage
If you run the program without any parameters it will print help.
<p align="center"><img src="Images/RunProgramWithoutParameters.jpg"></p>
Also you can find examples of using in the Scripts directory.  

Every time you run SynTex.exe with proper parameters it generates texture and writes data related to that texture to db.csv. Later you can use db2table.exe tool to generate table from it. Available option at the moment is MD file only.
<p align="center"><img src="Images/Pipeline.jpg"></p>

## Version history
#### ver 0.1
* Refactoring of program structure and code.
* Console application with command line arguments.
* Register new algorithms mechanism added.
* First texture synthesis algorithm full neighborhood search (FNS) added.
* Seed passing support.
* LogChecker added.
* db.csv writing added.

## If you want to contribute
There are several directions that are nice to support:
* Crossplatform project and build support 
* Generate google spreadsheet as another output from db2table tool