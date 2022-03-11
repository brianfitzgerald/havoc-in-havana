# /bin/bash

aws s3 cp E:\ttt_build\webgl_build s3://ttt-site/ --recursive --acl public-read
aws s3 cp ~/projects/ttt_build/webgl_build/ s3://ttt-site/ --recursive --acl public-read --profile personal