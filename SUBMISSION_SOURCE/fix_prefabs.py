import os
import re

folder = r'D:\Unity\Zatun_Test_Nathanieal\Assets'
meta_guids = set()
for root, _, files in os.walk(folder):
    for f in files:
        if f.endswith('.meta'):
            with open(os.path.join(root, f), 'r', encoding='utf-8', errors='ignore') as fp:
                for line in fp:
                    if line.startswith('guid:'):
                        meta_guids.add(line.split('guid:')[1].strip())
                        break

def clean_prefab(filepath):
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()

    missing_guids = []
    matches = re.findall(r'm_Script: \{fileID:.*?, guid: ([a-f0-9]{32}),', content)
    for g in matches:
        if g not in meta_guids:
            missing_guids.append(g)
            
    has_fileid0 = 'm_Script: {fileID: 0}' in content
    if not missing_guids and not has_fileid0:
        return False
        
    print(f"Cleaning {filepath}")
    
    ids_to_remove = []
    
    for missing_guid in set(missing_guids):
        pattern = r'--- !u!114 &(\d+)\r?\nMonoBehaviour:[\s\S]*?m_Script: \{fileID: \d+, guid: ' + missing_guid + r', type: 3\}[\s\S]*?(?=--- !u!|\Z)'
        for match in re.finditer(pattern, content):
            comp_id = match.group(1)
            ids_to_remove.append(comp_id)
            print(f"  Removing component id {comp_id} (missing guid {missing_guid})")
            
        content = re.sub(pattern, '', content)

    if has_fileid0:
        pattern = r'--- !u!114 &(\d+)\r?\nMonoBehaviour:[\s\S]*?m_Script: \{fileID: 0\}[\s\S]*?(?=--- !u!|\Z)'
        for match in re.finditer(pattern, content):
            comp_id = match.group(1)
            ids_to_remove.append(comp_id)
            print(f"  Removing component id {comp_id} (fileID: 0)")
            
        content = re.sub(pattern, '', content)

    for comp_id in ids_to_remove:
        comp_pattern = r'\s*- component: \{fileID: ' + comp_id + r'\}\r?\n'
        content = re.sub(comp_pattern, '\n', content)

    with open(filepath, 'w', encoding='utf-8') as f:
        f.write(content)
        
    return True

for root, _, files in os.walk(folder):
    for f in files:
        if f.endswith('.prefab'):
            p = os.path.join(root, f)
            try:
                clean_prefab(p)
            except Exception as e:
                print(f"Error on {p}: {e}")
