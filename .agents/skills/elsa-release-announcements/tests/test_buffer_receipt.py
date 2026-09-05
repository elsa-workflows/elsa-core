from pathlib import Path
import sys
import tempfile
from types import SimpleNamespace
import unittest
from datetime import datetime,timezone
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'scripts'))
import buffer_receipt as helper


class BufferReceiptTests(unittest.TestCase):
    def setUp(self):
        temporary=tempfile.TemporaryDirectory();self.addCleanup(temporary.cleanup)
        self.root=Path(temporary.name)
        self.message=self.root/'message.txt';self.message.write_text('Elsa 3.9.0 is available')
        self.args=SimpleNamespace(state_file=self.root/'state.json',platform='linkedin',channel_id='channel',message_file=self.message,absence_evidence=None)

    def test_resume_requires_reconciliation_and_known_id_cannot_be_discarded(self):
        self.assertEqual('publish',helper.begin(self.args)['action'])
        self.assertEqual('reconcile',helper.begin(self.args)['action'])
        self.args.post_id='post';helper.note_id(self.args)
        self.args.absence_evidence=self.root/'absence.json'
        with self.assertRaisesRegex(ValueError,'known post ID'):helper.begin(self.args)

    def test_confirmed_absence_allows_retry_but_changed_target_does_not(self):
        helper.begin(self.args);self.args.absence_evidence=self.root/'absence.json'
        state=helper.read(self.args.state_file)
        helper.save(self.args.absence_evidence,{'observed_at':datetime.now(timezone.utc).isoformat(),'pages':[{'request':{'channelIds':['channel'],'createdAt':{'start':state['created_at']}},'response':{'edges':[],'pageInfo':{'hasNextPage':False,'endCursor':None}}}]})
        self.assertEqual('publish',helper.begin(self.args)['action'])
        self.args.channel_id='wrong'
        with self.assertRaisesRegex(ValueError,'differs'):helper.begin(self.args)

    def test_incomplete_or_matching_reconciliation_cannot_authorize_repost(self):
        helper.begin(self.args)
        self.args.absence_evidence=self.root/'absence.json'
        state=helper.read(self.args.state_file)
        evidence={'observed_at':datetime.now(timezone.utc).isoformat(),'pages':[{'request':{'channelIds':['channel'],'createdAt':{'start':state['created_at']}},'response':{'edges':[],'pageInfo':{'hasNextPage':True,'endCursor':'more'}}}]}
        helper.save(self.args.absence_evidence,evidence)
        with self.assertRaisesRegex(ValueError,'incomplete'):
            helper.begin(self.args)
        response=evidence['pages'][0]['response'];response['pageInfo']['hasNextPage']=False
        response['edges']=[{'node':{'id':'existing','channelId':'channel','text':self.message.read_text()}}]
        helper.save(self.args.absence_evidence,evidence)
        with self.assertRaisesRegex(ValueError,'matching post exists'):
            helper.begin(self.args)

    def test_only_exact_sent_connector_post_produces_receipt(self):
        helper.begin(self.args)
        self.args.response_file=self.root/'get-post.json';self.args.receipt_file=self.root/'receipt.json'
        post={'id':'post','channelId':'channel','text':self.message.read_text(),'externalLink':'https://linkedin.com/post','sentAt':'2026-09-05','status':'sending'}
        helper.save(self.args.response_file,post)
        with self.assertRaisesRegex(ValueError,'verified sent'):helper.record(self.args)
        post['status']='sent';post['channelId']='wrong';helper.save(self.args.response_file,post)
        with self.assertRaisesRegex(ValueError,'differs'):helper.record(self.args)
        post['channelId']='channel';helper.save(self.args.response_file,post)
        receipt=helper.record(self.args)
        self.assertEqual('sent',receipt['status'])
        self.assertEqual('verify-existing',helper.begin(self.args)['action'])
        self.assertEqual(receipt,helper.record(self.args))


if __name__=='__main__':unittest.main()
