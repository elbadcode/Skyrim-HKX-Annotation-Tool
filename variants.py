import os
import json
import sys


def organize_files(folder_path):
    """
    Scans a folder, moves files to corresponding variant folders,
    iterating for duplicates.
    """
    for file in os.scandir(folder_path):
        # Extract base filename without extension
        base_filename, ext = os.path.splitext(file.name)

        # Skip non-hkx files
        if ext.lower() != ".hkx":
            continue

        # Construct variant folder name
        variant_folder = f"_variants_{base_filename}"

        # Construct destination path with incrementing number for duplicates
        destination_path = os.path.join(variant_folder, "1.hkx")
        i = 1
        while os.path.exists(destination_path):
            i += 1
            destination_path = os.path.join(variant_folder, f"{i}.hkx")

        # Create variant folder if it doesn't exist
        if not os.path.exists(variant_folder):
            os.makedirs(variant_folder)

        # Move the file to the destination path
        source_path = os.path.join(folder_path, file.name)
        os.rename(source_path, destination_path)

    print(f"Finished organizing files in {folder_path}")


directory = sys.argv[1] if len(sys.argv) > 1 else os.getcwd()
if directory == os.getcwd():
    os.chdir(os.path.dirname(os.getcwd()))
directories = [os.path.abspath(x[0]) for x in os.walk(directory)]
directories.remove(os.path.abspath(directory))  #
for i in directories:
    if "variants" not in i:
        os.chdir(i)
        print(i)
        organize_files(i)

repl_anim_list = []
for d in os.scandir(directory):
    if os.path.isdir(d.path):
        hasanim = False
        for f in os.scandir(d.path):
            if f.name.endswith(".hkx"):
                hasanim = True
        else:
            if not hasanim:
                continue
            repl_anim_list.append("data\\meshes" + d.path.split("meshes")[1])
for a in repl_anim_list:
    print(a)

repl_anim_keys = []
for anim in repl_anim_list:
        {
    keys = [
            "projectName": "DefaultMale",
            "path": anim,
            "variantMode": 0,
            "variantStateScope": 2,
            "blendBetweenVariants": True,
            "resetRandomOnLoopOrEcho": False,
            "sharePlayedHistory": True,
        },
        {
            "projectName": "DefaultFemale",
            "path": anim,
            "variantMode": 0,
            "variantStateScope": 2,
            "blendBetweenVariants": True,
            "resetRandomOnLoopOrEcho": False,
            "sharePlayedHistory": True,
        },
    ]
    for key in keys:
        repl_anim_keys.append(key)

with open(os.path.join(directory, "config.json"), "r+") as f:
    config_data = json.load(f)
    keys = list(config_data.keys())
    if "replacementAnimDatas" not in keys:
        keys.insert(2, "replacementAnimDatas")
    config_data["replacementAnimDatas"] = repl_anim_keys
    f.seek(0)
    json.dump(config_data, f, indent=2)
    f.truncate()
