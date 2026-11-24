"""Localizable call previews inside dialogue metadata; animation definitions stay literal."""
from corpus import *

CALL_RE=re.compile(r'^(passengercall|passengerall|passenger-call)\s*:\s*(.*)$',re.I)

def passenger_calls(user_datas):
    result=[]
    for index,raw in enumerate(user_datas):
        match=CALL_RE.match(raw)
        if not match:continue
        text=match.group(2)
        if text.strip().upper() in {'N/A','NO PASSENGERCALL FOR THIS SCENE.'}:continue
        result.append(dict(index=index,english=text,prefix=raw[:match.start(2)],original=raw))
    return result

def reviewed_calls(name,user_datas,supplied=None):
    calls=passenger_calls(user_datas)
    if supplied is None:
        path=ROOT/'localization/passenger_calls.json'
        supplied=read_json(path).get(name,{}) if path.exists() else {}
    assert set(supplied)=={str(c['index']) for c in calls},(name,'missing/extra call previews',supplied,calls)
    result=[]
    for c in calls:
        row=dict(c,russian=supplied[str(c['index'])])
        # This exact owner/literal is source-proven metadata, unlike useful
        # invitations on other cards whose has_passenger_call is also zero.
        if name=='054_ludivine_01' and c['english']=='NO PASSENGER CALL':
            assert row['russian']==c['english'],(name,'Technical no-call literal must stay unchanged')
            row.update(technical_marker=True,decision='Источник054/PassengerObjectScript2378 has_passenger_call0/has_travel0 и буквальное NO PASSENGER CALL: техническая заглушка, не приглашение. Метка сохраняется при каждом save_scene.')
        result.append(row)
    return result

def apply_calls(user_datas,calls):
    result=list(user_datas)
    for c in calls:
        assert result[c['index']]==c['original'],('call source differs',c)
        result[c['index']]=c['prefix']+c['russian']
    return result
