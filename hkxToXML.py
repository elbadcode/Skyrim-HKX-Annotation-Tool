import ctypes,os,sys, pathlib, annotation_edit, time


hkxconv = "C:\\Games\\hkxconv.exe"


def recursePath(path = None):
    for root,dirs,files in os.walk(path):
        hkx_files = [f for f in files if f.endswith('.hkx')]

hkxList = [""]
xmlList = [""]


def do_conv(file_path):
    try:
        if os.path.isfile(file_path):

            file_name = file.split('.hkx')[0].rsplit("\\")[1]
            file_rep = file_name + '-repl.txt'
            file_out = file_name + '.xml'
            if os.path.exists(file_out):
                print(file_out)
                if os.path.exists(file_rep):
                    file_done = annotation_edit.replace_annotations(file_out, file_rep)
                    return file_out
                return file_out
            os.chdir(os.path.dirname(file))
            subprocess.run([hkxconv, file_name, file_out])
            os.system(cmd)
            time.sleep(5)
            print(cmd)
            if os.path.exists(file_out):
                print(fileout)
                return file_out
            else:
                subprocess.run([hkxcmd, file_name, "-v:amd64"])
                file_new = file_name + '-out.hkx'
                os.system(cmd2)
                time.sleep(5)
                cmd = f'hkxconv convert -v xml "{file_new}" "{file_out}"'
                if os.path.exists(file_new):
                    os.system(cmd)
                    if os.path.exists(file_out):
                        return file_out
    except Exception as e:
        print(e)

def recurse_count(folder):
    folder_ct = 0
    for file_entry in os.scandir(folder):
        file = file_entry.path
        fullname = os.path.join(folder, file)
        if os.path.isfile(fullname):
            if file.endswith('.hkx'):
                print(file)
                if file.startswith("MCO"):
                    folder_ct += 1
                    hkxList.append(file)
                    print(f"file: {file}")
                    out = do_conv(file)
                    print(f"out: {out}")
                    xmlList.append(out)
                elif "_variants_mco" in str(file):
                    folder_ct += 1
                    hkxList.append(file)
                    print(f"file: {file}")
                    out = do_conv(file)
                    print(f"out: {out}")
                    xmlList.append(out)
        elif os.path.isdir(fullname):
            folder_ct += recurse_count(fullname)
    print(f"{folder}: {folder_ct}")
    return folder_ct


for mod in os.scandir("F:\\nefa\\mods"):
    try:
        recurse_count(f"F:\\nefa\\mods\\{mod.name}\\meshes\\actors\\character\\animations\\OpenAnimationReplacer")
    except Exception as e:
        pass