# Proof of concept!
##  First implementation of a very trivial fade in fade out tool

mp3 source (Creative commons License): https://www.bensound.com/royalty-free-music/track/summer-chill-relaxed-tropical

works only on Windows (for now)

## TODOS
* Reuse the same Mp3Reader (var reader = GetMp3FileReaderFromFilePath(infile)) for all steps to increase performance?
* Use Constant bitrate instead of Average bitrate. Turn off run-length encoding (RLE)??? Reason: To remove the crackling noise at the transition phases.
* Clean Code!
* improve benchmarking
* usage of multiple input files and single output file
* improve configurability