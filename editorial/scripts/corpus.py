from pathlib import Path
import collections, json, re, unicodedata
ROOT = Path(__file__).resolve().parents[1]

REVIEWED = ROOT.parent / 'translation/reviewed'
UI_KEYS = ROOT.parent / 'translation/ui_ru.json'

def read_json(path):
    return json.loads(Path(path).read_text(encoding='utf-8-sig'))

def write_json(path, data):
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')

def original_dialogues():
    inv = read_json(ROOT / 'original/inventory.json')
    return [read_json(ROOT / x['file']) for x in inv if x['cls'] == 'DialogObjectScript']

def original_ui():
    inv = read_json(ROOT / 'original/inventory.json')
    loc = next(read_json(ROOT / x['file']) for x in inv
               if x['cls'] == 'LocalizationData' and x['name'] == 'LocalizationDataENG')
    return {x['_key']: x['_value'] for x in loc['_items']}

def normalize(s):
    return s.translate(str.maketrans({'’': "'", '‘': "'", '“': '"', '”': '"',
                                    '…': '...', '\u00a0': ' ', '–': '-', '—': '-'})).strip()

def parse_text(path):
    content = Path(path).read_text(encoding='utf-8-sig')
    return parse_text_content(content)


def parse_text_content(content):
    """Parse a source value directly, without a shared scratch file."""
    titles = {s[3:].strip() for s in content.splitlines() if s.startswith('===')}
    blocks, current, duplicates = {}, None, []
    for number, line in enumerate(content.splitlines(), 1):
        if line.startswith('==='):
            name = line[3:].strip()
            if name in blocks:
                duplicates.append(name)
            current = dict(title=name, lines=[], choices=[], line_numbers=[])
            blocks[name] = current
            continue
        if current is None:
            continue
        s = line.strip()
        if not s or s.startswith('%%'):
            continue
        if s.startswith('*'):
            text, sep, target = s[1:].strip().rpartition('->')
            if sep:
                current['choices'].append(dict(text=text.strip(), link=target.strip(), number=number))
            continue
        if (s.startswith('{') and s.endswith('}') or s.startswith('->') or s in titles
            or ';;' in s or re.match(r'^:[a-z]+:', s)):
            continue
        current['lines'].append(s)
        current['line_numbers'].append(number)
    return blocks, duplicates


def cache_source_text(path, content):
    """Save exact snapshots without exposing truncated files to other readers."""
    import os, tempfile
    path = Path(path)
    data = content.encode('utf-8')
    if path.exists() and path.read_bytes() == data:
        return
    path.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.NamedTemporaryFile(dir=path.parent, delete=False) as out:
        temporary = Path(out.name)
        out.write(data)
    try:
        os.replace(temporary, path)
    finally:
        temporary.unlink(missing_ok=True)

def obj_base(name):
    name = re.sub(r'_(eng|fre|ger)$', '', name)
    return re.sub(r'_\d+$', '', name)

def russian_lookup(data):
    qualified, global_blocks, dup = {}, {}, []
    for path in sorted((Path(data) / 'Russian_Texts').glob('*.txt')):
        blocks, duplicates = parse_text(path)
        base = path.stem.replace('_rus', '')
        for title, b in blocks.items():
            b = dict(b, file=path.relative_to(ROOT).as_posix(), base=base)
            qualified[(base, title)] = b
            global_blocks.setdefault(title, b)
        dup.extend(dict(file=str(path), passage=x) for x in duplicates)
    return qualified, global_blocks, dup

def get_block(qualified, fallback, name, title):
    exact = re.sub(r'_(eng|fre|ger)$', '', name)
    return qualified.get((exact, title)) or qualified.get((obj_base(name), title)) or fallback.get(title)

def emote(s):
    m = re.match(r'^:[a-z]+:', s)
    return m[0] if m else ''

def command(s):
    return s.strip().startswith('$$')

def speaker(s):
    m = re.match(r'^([A-ZÀ-ŸА-ЯЁ\- ]{2,30})\s*:', s)
    return m[1].strip() if m else ''

def visible(s):
    return re.sub(r'^[A-ZÀ-ŸА-ЯЁ\- ]{2,30}\s*:\s*', '', s).strip()

def main():
    import argparse
    parser = argparse.ArgumentParser()
    parser.add_argument('--data', default='revision/data')
    parser.add_argument('--out', default='review')
    args = parser.parse_args()
    data = ROOT / args.data
    qualified, fallback, duplicates = russian_lookup(data)
    rows, findings, cases = [], [], []
    stats = collections.Counter()
    overrides_file = ROOT/'review/editorial_overrides.json'
    overrides = read_json(overrides_file) if args.data=='revision/data' and overrides_file.exists() else []
    reviewed_refs = {(x['object'],x['passage']) for x in overrides if x['reviewed']}
    if args.data=='revision/data' and (REVIEWED).exists():
        for path in (REVIEWED).glob('*.json'):
            r=read_json(path)
            reviewed_refs.update((r['name'],p['passage']) for p in r['passages'])
    changes_file = ROOT/'review/changes.json'
    reviewed_ui = {x['key'] for x in read_json(changes_file) if x['category']=='ui'} if args.data=='revision/data' and changes_file.exists() else set()
    for obj in original_dialogues():
        name = obj['m_Name']
        dialog = next(d['dialog'] for d in obj['dialogs'] if d['lang'] == 1)
        stats['objects'] += 1
        for p in dialog['_passages']:
            stats['passages'] += 1
            b = get_block(qualified, fallback, name, p['_title'])
            ref = dict(object=name, passage=p['_title'])
            source_lines = p['_lines']
            if b is None:
                if source_lines or p['_choices']:
                    findings.append(dict(ref, issue='no_russian_block', english=source_lines))
                continue
            rlines = b['lines']
            en_text = [x for x in source_lines if not command(x)]
            ru_text = [x for x in rlines if not command(x)]
            if len(en_text) != len(ru_text):
                findings.append(dict(ref, issue='visible_line_count', file=b['file'], english=en_text, russian=ru_text))
            ec = [normalize(x) for x in source_lines if command(x)]
            rc = [normalize(x) for x in rlines if command(x)]
            if ec != rc:
                findings.append(dict(ref, issue='command_sequence', file=b['file'], english=ec, russian=rc))
            stats['visible_lines'] += len(en_text)
            stats['choices'] += len(p['_choices'])
            if not source_lines and p['_choices']:
                cases.append(dict(ref, russian_file=b['file'], choices=p['_choices']))
            for i in range(max(len(en_text), len(ru_text))):
                rows.append(dict(ref, kind='line', ordinal=i,
                                 english=en_text[i] if i < len(en_text) else '',
                                 russian=ru_text[i] if i < len(ru_text) else '',
                                 alignment='candidate_by_position', reviewed=(name,p['_title']) in reviewed_refs, file=b['file']))
            link_groups = collections.defaultdict(list)
            for c in b['choices']:
                link_groups[c['link']].append(c['text'])
            used = collections.Counter()
            for c in p['_choices']:
                link = c['_link']
                n = used[link]
                used[link] += 1
                text = link_groups[link][n] if n < len(link_groups[link]) else ''
                if not text:
                    findings.append(dict(ref, issue='missing_local_choice', link=link, english=c['_text']))
                elif emote(c['_text']) != emote(text):
                    findings.append(dict(ref, issue='emote_difference', link=link, english=c['_text'], russian=text))
                rows.append(dict(ref, kind='choice', ordinal=n, link=link, english=c['_text'], russian=text,
                                 alignment='by_target_within_passage', reviewed=(name,p['_title']) in reviewed_refs, file=b['file']))
    ru_ui = read_json(UI_KEYS if args.data=='revision/data' else data / 'Russian_UI/key_based_translations.json')
    fr_ui_path=ROOT/'original/LocalizationData/sharedassets0.assets/2097.json'
    fr_ui={x['_key']:x['_value'] for x in read_json(fr_ui_path)['_items']} if fr_ui_path.exists() else {}
    for key, eng in original_ui().items():
        ru = ru_ui.get(key, '')
        if not ru and (eng.strip() or fr_ui.get(key,'').strip()):
            findings.append(dict(issue='missing_ui_key', key=key, english=eng))
        if ru and collections.Counter(re.findall(r'\{\d+(?:[^{}]*)\}', eng)) != collections.Counter(re.findall(r'\{\d+(?:[^{}]*)\}', ru)):
            findings.append(dict(issue='format_placeholder', key=key, english=eng, russian=ru))
        rows.append(dict(kind='ui', key=key, english=eng, russian=ru, reviewed=key in reviewed_ui))
    out = ROOT / args.out
    out.mkdir(exist_ok=True, parents=True)
    with (out / 'corpus.jsonl').open('w', encoding='utf-8') as f:
        for row in rows:
            f.write(json.dumps(row, ensure_ascii=False) + '\n')
    write_json(out / 'findings.json', findings)
    write_json(out / 'choices_only_passages.json', cases)
    write_json(out / 'duplicate_titles.json', duplicates)
    write_json(out / 'stats.json', dict(stats, ui_keys=len(original_ui()),
        findings=dict(collections.Counter(x['issue'] for x in findings)),
        choice_only_passages=len(cases), rows=len(rows)))
    print(json.dumps(read_json(out / 'stats.json'), ensure_ascii=False, indent=2))

if __name__ == '__main__':
    main()
