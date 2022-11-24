find . -iname "bin" -print0 | xargs -0 rm -rf
find . -iname "obj" -print0 | xargs -0 rm -rf
find . -iname "server" -print0 | xargs -0 rm -rf
find . -iname "nupkg" -print0 | xargs -0 rm -rf