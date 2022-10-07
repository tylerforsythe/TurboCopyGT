TurboCopyGT
====

The purpose of this CLI app is simple: be a very fast, multi-threaded copy program on Windows 
(and probably other platforms that run .NET).

**NOTE: SSDs required.** *This program is useless if you're not using high quality SSDs or you're only copying very 
large files. If you're still using spinning platters, don't bother.*

Invoke it with `mode`, `source-path`, and `destination-path` parameters and let it rip. See below.
It will spin up many threads based on your CPU count and try to copy at 100% CPU utilization. 
It is not particularly smart or efficient, but it will copy **MUCH** faster than Windows default file copy for many small files. 
Expect it to be thread-count-times faster than Windows. So if you have 16 threads, expecting ~16x faster
file copies is realistic for directories with many files. 
If you're trying to copy a single large file faster, that's not possible except to upgrade your drives.

Why not just use robocopy? Robocopy is a great piece of software that is fast and very configurable, 
but it is not multi-threaded and has no source code available.

Usage
-----

`TurboCopyGT.exe --mode Copy --source-path C:\SourceDirectory\ --destination-path D:\DesinationDirectory\`

How It Works
-----

In a nutshell, this program examines all directories in `source-path` and creates all of them in `destination-path`,
then it stores in-memory every file path in `source-path`, splits that list into equal-count chunks based on 
the number of threads available, and spins up a file-copy thread with each chunk. Then it waits for all threads to finish. 

This generally works really well, but there are limitations. For example, it does not query file size when it compiles 
the initial file list. So if your file size distribution happens to have many large files in thread 1 and many small files 
in thread 2, you will not get optimal copy speeds. Faster than default Windows, but not optimal.

Additionally, if your data set is mostly very large files, you will not see a speed increase. As stated at the top, 
this does not improve *sequential* read and write speeds. It excels at directories full of small files, which is the 
scenario Windows is terrible at. Windows file copy works fine for large files, but poorly for large sets of very small files.