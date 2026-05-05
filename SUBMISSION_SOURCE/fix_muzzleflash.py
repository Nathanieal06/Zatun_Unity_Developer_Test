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
            
    if not missing_guids:
        return False
        
    print(f"Cleaning {filepath}, found missing guids: {missing_guids}")
    
    parts = re.split(r'(^--- !u!.*?$)', content, flags=re.MULTILINE)
    
    new_parts = [parts[0]]
    ids_to_remove = []
    
    for i in range(1, len(parts), 2):
        delimiter = parts[i]
        obj_content = parts[i+1]
        
        is_missing = False
        
        if 'MonoBehaviour:' in obj_content:
            m = re.search(r'm_Script: \{fileID: \d+, guid: ([a-f0-9]{32}),', obj_content)
            if m and m.group(1) in missing_guids:
                is_missing = True
                
        if is_missing:
            id_match = re.search(r'&(\d+)', delimiter)
            if id_match:
                comp_id = id_match.group(1)
                ids_to_remove.append(comp_id)
                print(f"  Removing component id {comp_id}")
            continue
            
        new_parts.append(delimiter)
        new_parts.append(obj_content)
        
    new_content = "".join(new_parts)
    
    for comp_id in ids_to_remove:
        comp_pattern = r'\s*- component: \{fileID: ' + comp_id + r'\}\r?\n'
        new_content = re.sub(comp_pattern, '\n', new_content)

    with open(filepath, 'w', encoding='utf-8') as f:
        f.write(new_content)
        
    return True

clean_prefab(r'D:\Unity\Zatun_Test_Nathanieal\Assets\Prefabs\Effects\MuzzleFlash.prefab')
