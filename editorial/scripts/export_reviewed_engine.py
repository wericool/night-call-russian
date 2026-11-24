"""Prepare current editorial data for standalone checks using the game's Prompter.dll."""
from full_review import *
import copy

def main():
    sources=source_records();result={}
    from french_sources import french_source_records,validate_french_review
    french=french_source_records()
    for path in (REVIEWED).glob('*.json'):
        r=read_json(path);s=sources[r['name']]
        if s['kind']!='compiled':continue
        validate_french_review(r['name'],r.get('french_review'),s,french_source=french[r['name']])
        data=read_json(ROOT/s['original_file'])
        dialog=copy.deepcopy(next(x['dialog'] for x in data['dialogs'] if x['lang']==1))
        units={u['passage']:u for u in r['passages']}
        for p in dialog['_passages']:
            p.update(apply_compiled_review(s,r,p))
        dialog['_user_datas']=apply_calls(dialog['_user_datas'],r.get('passenger_calls',[]))
        result[r['name']]=dialog
    write_json(DEST/'reviewed_engine_input.json',result)
    print('Exported',len(result),'reviewed compiled scenes for the real Prompter runtime')

if __name__=='__main__':main()
