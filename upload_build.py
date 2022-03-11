import platform
import jinja2
from jinja2 import Environment, PackageLoader, select_autoescape
import os
import stat
import shutil

from jinja2.loaders import FileSystemLoader

pf = platform.system()
unity_executable = r'"C:\Program Files\Unity\Hub\Editor\2021.2.3f1\Editor\Unity.exe"' if pf == "Windows" else "/Applications/Unity/Hub/Editor/2021.2.7f1/Unity.app/Contents/MacOS/Unity"

project_path = os.path.dirname(os.path.realpath(__file__))
home = os.path.expanduser('~')

build_loc = r"E:\\ttt_build\\" if pf == "Windows" else home + "/ttt_build/"

version_loc = os.path.join(project_path, "Assets", "Resources", "Version.txt")
version = open(version_loc, 'r').read()

# only compress and upload for mac
only_upload_mac = True

for idx, cmd in enumerate([(r"-buildWindows64Player", r"\windows\Havoc.exe", "windows"), (r"-buildOSXUniversalPlayer", os.path.join(build_loc, r"/mac/Havoc.app"),"mac")]):
    if idx == 0 and only_upload_mac:
        continue
    if not only_upload_mac:
        command_to_run = "{} {} {} -project_path {} -quit -batchmode".format(unity_executable, cmd[0], cmd[1], project_path)
        os.system(command_to_run)
    print(home, build_loc, cmd[2])
    print("Compressing: " + cmd[2])
    out_dir = build_loc + cmd[2]
    shutil.make_archive(build_loc + cmd[2] , 'zip', out_dir)

for plat in ["mac", "windows"]:
    if plat == "mac" or not only_upload_mac:
        zip_loc = build_loc + plat + ".zip"
        command_to_run = "aws s3 cp {} s3://ttt-builds/{}.zip --acl public-read --profile personal".format(zip_loc, plat)
        os.system(command_to_run)


# Update website

env = Environment(
    loader=FileSystemLoader("templates"),
    autoescape=select_autoescape()
)


template = env.get_template("homepage.html")
template.stream(version=version).dump("index.html")
for file in ["index.html", "bg.jpg"]:
    os.system("aws s3 cp {} s3://ttt-site --acl public-read --profile personal".format(file))