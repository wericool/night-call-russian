"""Reproduce the approved Cyrillic density on the 2026.10.28 native package.

Requires UnityPy and Pillow. Does not install anything into the game.
Only the two matching input assets are accepted; all other objects are retained.
"""
from pathlib import Path
import argparse, hashlib, json, math
import UnityPy
from UnityPy.helpers.TypeTreeGenerator import TypeTreeGenerator
from UnityPy.helpers.TypeTreeNode import TypeTreeNode
from PIL import Image, ImageDraw, ImageChops

TARGETS = {'sharedassets0.assets': [2078, 2079, 2080], 'sharedassets3.assets': [136]}
BASE_HASHES = {
    'sharedassets0.assets': 'c3ef84e3c3917833e589877856cb8fbe4ff7e13c2074e481974800438a3122be',
    'sharedassets3.assets': '2201ae23ddb1265cfa6cd66edc9388d1752dc37234f7436730c7a809f6e24ab7',
}
CYRILLIC = set(range(0x410, 0x450)) | {0x401, 0x451}
GAIN = 1.75
CONTOUR_OFFSET = 10.0
LUT = [max(0, min(255, round(128 + (v - 128) * GAIN + CONTOUR_OFFSET))) for v in range(256)]

def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()

def corrected_node(node):
    flat = node.to_dict_list()
    for i, d in enumerate(flat):
        if d['m_Name'] == 'm_Enabled':
            d['m_MetaFlag'] |= 16384
        if (d['m_Type'] == 'string' and i+3 < len(flat)
                and flat[i+1]['m_Type'] == 'Array' and flat[i+3]['m_Type'] != 'char'):
            d['m_Type'] = 'vector'
        d.pop('m_Children', None)
    return TypeTreeNode.from_list(flat)

def load(path, dependency_dir, gen):
    env = UnityPy.load(str(path))
    env.typetree_generator = gen
    env.load_file(str(dependency_dir / 'globalgamemanagers.assets'))
    return env, next(a for a in env.assets if a.name == path.name)

def build(input_data, output_data, dependency_dir):
    input_data, output_data, dependency_dir = [Path(p).resolve() for p in (input_data, output_data, dependency_dir)]
    if output_data in [input_data, dependency_dir] or output_data.is_relative_to(dependency_dir):
        raise ValueError('Output must be separate from input and installed game data')
    if output_data.exists():
        raise FileExistsError('Use a new output directory')
    for name, expected in BASE_HASHES.items():
        if sha(input_data/name) != expected:
            raise ValueError('Unsupported input asset: ' + name)
    gen = TypeTreeGenerator('2018.4.0f1')
    gen.load_local_dll_folder(str(dependency_dir/'Managed'))
    output_data.mkdir(parents=True)
    report = []
    for name, pids in TARGETS.items():
        _, asset = load(input_data/name, dependency_dir, gen)
        raw = {pid: o.get_raw_data() for pid, o in asset.objects.items()}
        changes = set()
        images = {}
        for pid in pids:
            obj = asset.objects[pid]
            d = obj.read_typetree(nodes=corrected_node(obj.generate_monobehaviour_node()))
            assert d['m_Name'].startswith('AvenirLTStd-Light') and d['atlas']['m_FileID'] == 0
            tid = d['atlas']['m_PathID']
            assert tid not in changes
            tex = asset.objects[tid].read()
            old_meta = asset.objects[tid].read_typetree()
            old = tex.image.getchannel('A')
            mask = Image.new('L', old.size, 0)
            draw = ImageDraw.Draw(mask)
            letters = [g for g in d['m_glyphInfoList'] if g['id'] in CYRILLIC]
            assert len(letters) == 66
            for g in letters:
                draw.rectangle((math.floor(g['x'])-10, math.floor(g['y'])-10,
                    math.ceil(g['x']+g['width'])+9, math.ceil(g['y']+g['height'])+9), fill=255)
            new = Image.composite(old.point(LUT), old, mask)
            diff = ImageChops.difference(old, new)
            assert diff.getbbox()
            assert not ImageChops.multiply(diff, ImageChops.invert(mask)).getbbox()
            for g in d['m_glyphInfoList']:
                if g['id'] in CYRILLIC or g['id'] == 32:
                    continue
                box=(math.floor(g['x'])-5, math.floor(g['y'])-5,
                     math.ceil(g['x']+g['width'])+5, math.ceil(g['y']+g['height'])+5)
                assert not diff.crop(box).getbbox(), (name, pid, g['id'])
            rgba=Image.new('RGBA', old.size, 0)
            rgba.putalpha(new)
            tex.set_image(rgba, target_format=1, mipmap_count=1)
            tex.save()
            images[tid]=(new,old_meta)
            changes.add(tid)
        destination=output_data/name
        destination.write_bytes(asset.save())
        _, checked=load(destination, dependency_dir, gen)
        assert set(raw)==set(checked.objects)
        actual={pid for pid,o in checked.objects.items() if o.get_raw_data()!=raw[pid]}
        assert actual==changes
        for tid,(image,old_meta) in images.items():
            assert checked.objects[tid].read().image.getchannel('A').tobytes()==image.tobytes()
            meta=checked.objects[tid].read_typetree()
            for key in old_meta:
                if key not in ['image data','m_StreamData']:
                    assert meta[key]==old_meta[key], (name,tid,key)
        assert sha(input_data/name)==BASE_HASHES[name]
        report.append({'asset':name,'before_sha256':BASE_HASHES[name],'patched_sha256':sha(destination),
                       'changed_texture_ids':sorted(changes),'all_other_objects_identical':True,
                       'glyph_metrics_and_materials_unchanged':True})
    return {'passed':True,'gain':GAIN,'contour_offset':CONTOUR_OFFSET,'files':report}

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--input-data',required=True,type=Path,help='Night Call_Data in the 2026.10.28 package')
    parser.add_argument('--output-data',required=True,type=Path,help='New separate output directory')
    parser.add_argument('--game-data',required=True,type=Path,help='Installed Night Call_Data; read-only type dependencies')
    parser.add_argument('--report',required=True,type=Path)
    args=parser.parse_args()
    report=build(args.input_data,args.output_data,args.game_data)
    args.report.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print('PASS: only four Cyrillic atlas textures changed; text and metrics preserved')

if __name__=='__main__':main()
