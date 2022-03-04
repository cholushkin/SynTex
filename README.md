## Intro
Forked from https://github.com/mxgmn/TextureSynthesis
Project created for Microsoft Visual Studio Community 2019 and tested on Windows platform.
The purpose of this project is to create convenient sandbox for different texture synthesis algorithms. 
Using SynTex you can create spreadsheets like this:

https://docs.google.com/spreadsheets/d/1nEQUO0bUovebiYAIHOwNSmBBPVxUi-7dbhS40ZSbJvs/edit?usp=sharing

## Usage
If you run the program without any parameters it will print help.
<p align="center"><img src="Images/RunProgramWithoutParameters.jpg"></p>
Also you can find examples of using in the Scripts directory.

Every time you run SynTex.exe with proper parameters it generates texture and writes data related to that texture to db.csv. Later you can use db2table.exe tool to generate table from it. Available options at the moment are MD file(also it could be a google spreadsheet).
<p align="center"><img src="Images/Pipeline.jpg"></p>

 

## Version history
#### ver 0.1
* Refactoring of program structure and code.
* Console application with command line arguments.
* Register new algorithms mechanism added.
* First texture synthesis algorithm full neighborhood search (FNS) added.
* Seed passing support.
* db.csv writing added.
