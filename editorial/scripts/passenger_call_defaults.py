"""Keep save_scene's fallback calls equal to the accepted per-scene wording.

This is an auxiliary-table integrity check, not a new semantic translation
oracle. It runs after scene authoring, so explicit save_scene calls can still
be authored before their fallback table is synchronized.
"""
from corpus import ROOT, REVIEWED, read_json


def scene_findings(name, calls, table):
    expected = {str(c['index']): c['russian'] for c in calls}
    actual = table.get(name, {})
    if not isinstance(actual, dict):
        return [dict(scene=name, passage='_user_datas', rule='passenger_call_defaults',
                     issue='invalid_call_defaults_map', status='reviewed_conflict')]
    return [dict(scene=name, passage='_user_datas/' + index,
                 rule='passenger_call_defaults', issue='stale_or_missing_call_default',
                 status='reviewed_conflict', reviewed=expected.get(index), default=actual.get(index))
            for index in sorted(set(expected) | set(actual))
            if index not in expected or index not in actual or expected[index] != actual[index]]


def consistency_findings():
    table = read_json(ROOT / 'localization/passenger_calls.json')
    result = []
    for path in sorted((REVIEWED).glob('*.json')):
        review = read_json(path)
        result += scene_findings(review['name'], review['passenger_calls'], table)
    return result


def guard_probes():
    calls = [dict(index=0, russian='«Не успел на последнее метро… Мне в Сент-Уэн».')]
    good = {'056_kader_01': {'0': calls[0]['russian']}}
    cases = [
        ('accepted_metro', '056_kader_01', calls, good, False),
        ('stale_train', '056_kader_01', calls,
         {'056_kader_01': {'0': '«Не успел на последний поезд. Мне в Сент-Уэн».'}}, True),
        ('missing_owner', '056_kader_01', calls, {}, True),
        ('missing_index', '056_kader_01', calls, {'056_kader_01': {}}, True),
        ('extra_index', '056_kader_01', calls,
         {'056_kader_01': dict(good['056_kader_01'], **{'1': 'Чужой заказ'})}, True),
        ('foreign_owner_is_separate', '056_kader_01', calls,
         dict(good, other_passenger={'0': 'Мне на последний поезд'}), False),
        ('another_actual_train', 'other_passenger', [dict(index=0, russian='На последний поезд')],
         {'other_passenger': {'0': 'На последний поезд'}}, False),
        ('technical_no_call', '054_ludivine_01', [dict(index=0, russian='NO PASSENGER CALL')],
         {'054_ludivine_01': {'0': 'NO PASSENGER CALL'}}, False),
        ('empty_scene', 'no_call', [], good, False),
        ('obsolete_empty_scene_entry', 'no_call', [], {'no_call': {'0': 'Старое приглашение'}}, True),
        ('punctuation_drift', 'owner', [dict(index=0, russian='«В Исси».')],
         {'owner': {'0': '«В Исси.»'}}, True),
        ('malformed_map', 'owner', [], {'owner': []}, True),
    ]
    return [dict(rule='passenger_call_defaults', text=label, expected_conflict=bad,
                 passed=bool(scene_findings(name, rows, table)) == bad)
            for label, name, rows, table, bad in cases]
