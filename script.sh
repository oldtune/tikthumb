#! /bin/bash
ffprobe -v quiet -print_format json -show_streams video.mp4
ffmpeg -loop 1 -i image.jpg -f lavfi -i anullsrc=channel_layout=stereo:sample_rate=44100 -ac 2 -c:a aac -b:a 128011 -c:v libx264 -level:v 31 -b:v 1109800 -pix_fmt yuv420p -t 3 -r 30 -profile:v main image.mp4
ffmpeg -f concat -safe 0 -i input -c copy output.mp4
