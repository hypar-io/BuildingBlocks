#!/bin/bash
projects=(
    "./EmergencyEgress"
    "./People"
    "./Walls/Walls"
    "./Core/CoreByEnvelope"
    "./Core/CoreBySketch"
    "./Core/CoreByLevels"
    "./Facade/FacadeByEnvelope"
    "./Foundation/FoundationByEnvelope"
    "./Grids/Grid"
    "./Envelope/EnvelopeBySketch"
    "./Envelope/EnvelopeBySite"
    "./Envelope/EnvelopeByCenterline"
    "./Site/SiteBySketch"
    "./Roof/RoofBySketch"
    "./Roof/RoofByDXF"
    "./Roof/Roof"
    "./Columns/ColumnsFromGrid"
    "./Columns/ColumnsByFloors"
    "./Rooms/RoomsByLevels"
    "./Rooms/PlanByProgram"
    "./Rooms/ProgramByCSV"
    "./Floors/FloorsByDXF"
    "./Floors/FloorsBySketch"
    "./Floors/FloorsByLevels"
    "./Floors/SubdivideSlab"
    "./Levels/LevelBySketch"
    "./Levels/SimpleLevelsByEnvelope"
    "./Levels/LevelsByEnvelope"
    "./Structure/StructureByEnvelope"
)

task() {
echo $project
cd $project
hypar update && hypar init
}

for project in ${projects[@]};
do
    task $project &
done
wait