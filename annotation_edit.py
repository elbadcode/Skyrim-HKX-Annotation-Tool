from lxml import etree as ET
import time
import re
import subprocess
import sys
from pathlib import Path
import pprint
import os
import json


def ext_change(file_path, ext):
    try:
        return os.path.splitext(file_path)[0] + ext
    except TypeError:
        p = str(file_path).rsplit('.')[0] + ext
    return p



def stripall(string, *args):
    for x in args:
        str(string).strip(str(x))
    return string

def dump_annotations(path):
    st = time.time()
    xml_file = path
    anno_text_list = []
    try:
        tree = ET.parse(xml_file)
        annotations = tree.xpath('//hkparam[@name="annotations"]')
        for annotation in annotations:
            hkobjects = annotation.findall('.//hkobject')
            for hkobject in hkobjects:
                hkparams = hkobject.findall('.//hkparam[@name="text"]')
                for hkparam in hkparams:
                    try:
                        text = hkparam.text
                        anno_text_list.append(text)
                    except Exception as e:
                        pass
        name = Path(xml_file).stem
        outname = f"{name}-dump.txt"
        with open(outname, "w") as f:
            print(anno_text_list[::], file=f)
    except ET.XMLSyntaxError:
        pass

def replace_annotations(path, _map):
    st = time.time()
    xml_file = path
    anno_rep = _map
    tree = ET.parse(xml_file)
    annotations = tree.xpath('//hkparam[@name="annotations"]')
    for annotation in annotations:
        hkobjects = annotation.findall('.//hkobject')
        for hkobject in hkobjects:
            try:
                hkparams = hkobject.findall('.//hkparam[@name="text"]')
                for hkparam in hkparams:
                    try:
                        text = hkparam.text
                        if text in anno_rep:
                            try:
                                print(text)
                                print(anno_rep[text])
                                hkparam.text = anno_rep[text]
                            except Exception as e:
                                print(e)
                        else:
                            pass
                    except Exception as e:
                        print(e)
            except Exception as e:
                pass
    modified_xml_content = ET.tostring(tree)
    modified_xml_content = modified_xml_content.decode('utf-8')
    with open(path, "w") as xml_path:
        xml_path.write(modified_xml_content)
        hkx = str(path).split('.xml')[0] + '.hkx'
        cmd = f'hkxconv convert -v hkx "{path}" "{hkx}"'
        cmdstr = str(cmd)
        print(cmdstr)
        print(cmd)
        subprocess.run(cmdstr)
        et = time.time()
        elap = et - st
        print('Execution time: ', elap, ' seconds')
        return elap


def replace_annotations_ordered(path, _map):
    st = time.time()
    xml_file = path
    anno_rep = _map
    tree = ET.parse(xml_file)
    annotations = tree.xpath('//hkparam[@name="annotations"]')
    i = 0
    for annotation in annotations:
        hkobjects = annotation.findall('.//hkobject')
        for hkobject in hkobjects:
            hkparams = hkobject.findall('.//hkparam[@name="text"]')
            for hkparam in hkparams:
                # Get the text of the hkparam tag
                try:
                    text = hkparam.text
                    hkparam.text = anno_rep[i]
                    i += 1
                except Exception as e:
                    print(e)
    modified_xml_content = ET.tostring(tree)
    modified_xml_content = modified_xml_content.decode('utf-8')
    with open(path, "w") as xml_path:
        xml_path.write(modified_xml_content)
        hkx = str(path).split('.xml')[0] + '.hkx'
        cmd = f'hkxconv convert -v hkx "{path}" "{hkx}"'
        cmdstr = str(cmd)
        print(cmdstr)
        print(cmd)
        subprocess.run(cmdstr)
        et = time.time()
        elap = et - st
        print('Execution time: ', elap, ' seconds')
        return elap



def insert_annotations(path, _map):
    st = time.time()
    xml_file = path
    anno_rep = _map
    tree = ET.parse(xml_file)
    annotations = tree.xpath('//hkparam[@name="annotations"]')
    count = len(_map)
    i = 0
    for annotation in annotations:
        hkobjects = annotation.findall('.//hkobject')
        for hkobject in hkobjects:
            numanno = hkobject.findall('.//hkparam[@numelements="num"]')
            num = num + count
            hkparams = hkobject.findall('.//hkparam[@name="text"]')
            for hkparam in hkparams:
                # Get the text of the hkparam tag
                try:
                    text = hkparam.text
                    hkparam.text = anno_rep[i]
                    i += 1
                except Exception as e:
                    print(e)
    modified_xml_content = ET.tostring(tree)
    modified_xml_content = modified_xml_content.decode('utf-8')
    with open(path, "w") as xml_path:
        xml_path.write(modified_xml_content)
        hkx = str(path).split('.xml')[0] + '.hkx'
        cmd = f'hkxconv convert -v hkx "{path}" "{hkx}"'
        cmdstr = str(cmd)
        print(cmdstr)
        print(cmd)
        subprocess.run(cmdstr)
        et = time.time()
        elap = et - st
        print('Execution time: ', elap, ' seconds')
        return elap

def convert_hkx(path):
    name =  os.path.splitext(path)[0] + '.xml'
    subprocess.call(["hkxconv.exe", "convert", path, name])
    return name

def replace_regex(path, _regex, _subst):
    st = time.time()
    regex_anno =  _regex
    subst_anno = _subst
    with open(path, "r+", encoding='us-ascii') as xml_file:
        xml_content = xml_file.read()
        xml_content = re.sub(regex_anno, subst_anno, xml_content)
        print(f'{xml_content}',file=xml_file)  # Parse the modified XML content
        hkx = ext_change(path, '.hkx')
        cmd = f'hkxconv convert -v hkx "{path}" "{hkx}"'
        cmdstr = str(cmd)
        print(cmdstr)
        print(cmd)
        subprocess.run(cmdstr)
        et = time.time()
        elap = et - st
        print('Execution time: ', elap, ' seconds')
        return elap


def command_startup():
    #command = ["dump","replace","insert", "set"]
    command_access = -1
    rep = ""
    dump = ""
    infile = ""
    repmap = ""
    insert = ""
    dir = ""
    try:
        for arg in sys.argv:
            if 'dump' in arg:
                command_access = 0
            elif arg in ['replace','rep']:
                command_access = 1
            elif arg in ['ins', 'insert']:
                command_access = 2
            elif arg in ['set']:
                command_access = 3
            elif arg.endswith('.hkx'):
                infile = convert_hkx(arg)
            elif arg.endswith('.xml'):
                infile = arg
            elif arg.endswith('dump.txt'):
                rep = arg
            elif arg.endswith('rep.txt'):
                rep = arg
            elif arg.endswith('rep.json'):
                repmap = arg
            elif os.path.isdir(arg):
                dir = arg
    except Exception as e:
        print(e)

    if command_access == -1:
        if infile:
            dump = infile





    elif command_access == 0:
        dump_annotations(infile)

    elif command_access == 1 and rep:
        with open(rep, "r+") as f:
            rep_map = stripall(f.read(),{'[','[',' ',"'"}).split(',')
            print(rep_map)
            replace_annotations_ordered(infile, rep_map)
    elif command_access == 1 and repmap:
        with open(repmap, "r+") as f:
            rep_map = json.loads(f)
            replace_annotations(infile, rep_map)
    elif command_access == 2 and insert:
        with open(insert, "r+") as f:
            rep_map = json.loads(f)
            insert_annotations(infile, rep_map)
    elif command_access == 3 and dump and rep:
        with open(dump, "r+") as f:
            annos = f.read().split(',')
            for anno in annos:
                pprint(anno)
                anno = input('replace with: ')
        with open(rep, "w") as r:
            r = annos

folder_path = "F:\\nefa\\crashDumps\\OpenAnimationReplacer"
st = time.time()
global retotal

anno_rep = {r"PIE\.@SGVI\|MCO_nextattack\|": "BFCO_NextIsAttack",
            r"PIE\.@SGVI\|MCO_nextpowerattack\|": "BFCO_NextIsPowerAttack", "MCO_WinOpen": "BFCO_NextWinStart",
            "MCO_recovery": "BFCO_Recovery", "MCO_PowerWinOpen": "BFCO_NextPowerWinStart",
            "MCO_PowerWinClose": "BFCO_DIY_EndLoop", "MCO_WinClose": "BFCO_DIY_EndLoop"}
retotal = 0
if not os.path.isdir(folder_path):
    folder_path = Path(folder_path).parent
os.chdir(folder_path)
path = Path.cwd()
files = (p.resolve().relative_to(Path.cwd()) for p in Path(path).glob("**/*.hkx") if "mco" in str(p).lower() and not "-old" in str(p).lower())
for file_path in files:
    try:
        p = ext_change(Path(file_path).resolve(), ".xml")
        if os.path.exists(p):
            try:
                os.rename(file_path, ext_change(file_path, "-old.hkx"))
                print(f"og name:{p}")
                replace_annotations(p,anno_rep)
            except Exception as e:
                print('not found')
    except Exception as e:
        pass
