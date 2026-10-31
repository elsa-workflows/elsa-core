#!/usr/bin/env python3
"""Persist intent and validate Buffer connector receipts; this helper never posts."""
import argparse
import fcntl
import hashlib
import json
import os
from pathlib import Path
import sys
import tempfile
from datetime import datetime, timezone


def sha(text):
    return hashlib.sha256(text.strip().encode()).hexdigest()


def read(path):
    return json.loads(Path(path).read_text())


def save(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.NamedTemporaryFile(mode='w',dir=path.parent,delete=False) as file:
        json.dump(value,file,indent=2)
        file.write('\n'); file.flush(); os.fsync(file.fileno())
        temporary=file.name
    os.replace(temporary,path)


def payload(value):
    if 'content' in value:
        texts = [c['text'] for c in value['content'] if c.get('type') == 'text']
        if len(texts) != 1:
            raise ValueError('Expected one connector JSON response')
        return json.loads(texts[0])
    return value


def timestamp(value):
    result = datetime.fromisoformat(value.replace('Z', '+00:00'))
    if result.tzinfo is None:
        raise ValueError('Reconciliation timestamps must include timezone')
    return result


def validate_absence(state, path):
    evidence = read(path)
    age = (datetime.now(timezone.utc) - timestamp(evidence['observed_at'])).total_seconds()
    if not 0 <= age <= 300:
        raise ValueError('Reconciliation evidence must be from a fresh completed query')
    pages = evidence.get('pages', [])
    if not pages:
        raise ValueError('Reconciliation requires actual paginated connector responses')
    cursor = None
    for index, page in enumerate(pages):
        request = page['request']
        if request.get('channelIds') != [state['channel_id']] or request.get('after') != cursor:
            raise ValueError('Reconciliation query has wrong channel or a pagination gap')
        if request.get('status') or request.get('tagIds') or request.get('dueAt'):
            raise ValueError('Reconciliation cannot exclude post statuses, tags or schedules')
        created = request['createdAt']
        if created.get('end') or timestamp(created['start']) > timestamp(state['created_at']):
            raise ValueError('Reconciliation query does not cover the complete creation window')
        response = payload(page['response'])
        for edge in response['edges']:
            post = edge['node']
            channel = post.get('channelId') or post.get('channel', {}).get('id')
            if channel != state['channel_id'] or not isinstance(post.get('text'), str):
                raise ValueError('Unexpected or incomplete post in reconciliation response')
            if sha(post['text']) == state['content_sha256']:
                raise ValueError('A matching post exists; record or inspect its ID instead of creating another')
        info = response['pageInfo']
        if info['hasNextPage']:
            cursor = info['endCursor']
            if not cursor or index == len(pages)-1:
                raise ValueError('Reconciliation pagination is incomplete')
        elif index != len(pages)-1:
            raise ValueError('Unexpected extra reconciliation pages')
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def begin(args):
    text = args.message_file.read_text().strip()
    if not text:
        raise ValueError('Empty announcement')
    identity = {'platform':args.platform,'channel_id':args.channel_id,'content_sha256':sha(text)}
    if args.state_file.exists():
        state=read(args.state_file)
        if any(state.get(k)!=v for k,v in identity.items()):
            raise ValueError('Existing intent differs from channel or content; inspect before changing it')
        if state['status']=='sent':
            return {'action':'verify-existing','post_id':state['post_id']}
        if not args.absence_evidence:
            return {'action':'reconcile','post_id':state.get('post_id'),'created_at':state['created_at']}
        if state.get('post_id'):
            raise ValueError('A known post ID must be inspected; absence cannot clear it')
        state['absence_evidence_sha256'] = validate_absence(state,args.absence_evidence)
    else:
        state=dict(identity,created_at=datetime.now(timezone.utc).isoformat())
    state['status']='intent'
    save(args.state_file,state)
    return {'action':'publish','channel_id':args.channel_id,'content_sha256':sha(text)}


def record(args):
    state=read(args.state_file)
    post=payload(read(args.response_file))
    if post.get('status')!='sent' or post.get('error') or not post.get('externalLink') or not post.get('sentAt') or not post.get('id'):
        raise ValueError('Connector result is not a verified sent post with a public link')
    if post.get('channelId')!=state['channel_id'] or sha(post.get('text',''))!=state['content_sha256']:
        raise ValueError('Connector post differs from the intended channel or text')
    if state.get('post_id') and state['post_id']!=post['id']:
        raise ValueError('A different post ID is already recorded')
    receipt={'id':post['id'],'url':post['externalLink'],'text':post['text'],'status':'sent','error':None,'sent_at':post['sentAt'],'platform':state['platform']}
    state.update(status='sent',post_id=post['id'],url=post['externalLink'])
    save(args.state_file,state)
    save(args.receipt_file,receipt)
    return receipt


def note_id(args):
    state=read(args.state_file)
    if not args.post_id or (state.get('post_id') and state['post_id']!=args.post_id):
        raise ValueError('Conflicting or empty post ID')
    state['post_id']=args.post_id
    save(args.state_file,state)
    return {'action':'verify-existing','post_id':args.post_id}


def main(argv=None):
    parser=argparse.ArgumentParser(description=__doc__)
    sub=parser.add_subparsers(dest='action',required=True)
    p=sub.add_parser('begin')
    p.add_argument('--platform',choices=['linkedin','x'],required=True)
    p.add_argument('--channel-id',required=True)
    p.add_argument('--message-file',type=Path,required=True)
    p.add_argument('--absence-evidence',type=Path,help='Fresh complete connector list_posts query evidence proving no matching post exists')
    p=sub.add_parser('record')
    p.add_argument('--response-file',type=Path,required=True)
    p.add_argument('--receipt-file',type=Path,required=True)
    p=sub.add_parser('note-id')
    p.add_argument('--post-id',required=True)
    for p in sub.choices.values():
        p.add_argument('--state-file',type=Path,required=True)
    args=parser.parse_args(argv)
    try:
        args.state_file=args.state_file.expanduser().resolve()
        args.state_file.parent.mkdir(parents=True,exist_ok=True)
        with args.state_file.with_suffix('.lock').open('a') as lock:
            fcntl.flock(lock,fcntl.LOCK_EX|fcntl.LOCK_NB)
            print(json.dumps(globals()[args.action.replace('-','_')](args),indent=2))
        return 0
    except (ValueError,KeyError,OSError) as error:
        print(f'error: {error}',file=sys.stderr)
        return 1


if __name__=='__main__':
    raise SystemExit(main())
