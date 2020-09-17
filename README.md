# HIXTool
A simple program to dump and rebuild HIX and HAR texture archives.

# Usage

To dump an HIX and HAR archive, use:
```
HIXTool.exe -x <hix file>
```
The HIX and HAR archive must be in the same directory and have the same file name, minus extension. If not, the program will fail.

To rebuild an HIX and HAR archive, use:
```
HIXTool.exe -b <directory>
```
All PNG files with a 16-character name will be read and built into the new archive, and an HIX file will be created as well, which both share the name of the directory given to the program. All filenames must be valid 8-byte hexadecimal hashes in order to build properly.
