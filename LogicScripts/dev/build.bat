echo off
set fileIn=%1
set fileOut=%2

customasm -f binstr -o ./binary/%fileOut%.txt %fileIn%
customasm -f binary -o ./binary/%fileOut%.bin %fileIn%
customasm -f annotatedbin -o ./annotated/%fileOut%.txt %fileIn%