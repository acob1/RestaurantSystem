"""Validate deployment structure locally; schema validation needs --schema PATH.
Install PyYAML/jsonschema in an isolated environment if not already available.
"""
from pathlib import Path
import argparse, json
import yaml
parser=argparse.ArgumentParser()
parser.add_argument('--schema')
args=parser.parse_args()
root=Path(__file__).resolve().parents[1]
blueprint=yaml.safe_load((root/'infrastructure/render/render.yaml').read_text())
assert len(blueprint['services'])==3
api,admin,n8n=blueprint['services']
assert api['runtime']=='docker' and api['healthCheckPath']=='/health/live'
assert admin['runtime']=='static' and admin['staticPublishPath']=='dist/restaurant-admin/browser'
assert all(s['branch']=='main' and s['autoDeployTrigger']=='off' for s in blueprint['services'])
assert {'type':'rewrite','source':'/*','destination':'/index.html'} in admin['routes']
assert all(e.get('value')=='true' for e in n8n['envVars'] if e['key'] in ['DB_POSTGRESDB_SSL_ENABLED','DB_POSTGRESDB_SSL_REJECT_UNAUTHORIZED'])
angular=json.loads((root/'frontend/restaurant-admin/angular.json').read_text())
assert angular['projects']['restaurant-admin']['architect']['build']['options']['outputPath']=='dist/restaurant-admin'
yaml.safe_load((root/'.github/workflows/ci.yml').read_text())
if args.schema:
 import jsonschema
 jsonschema.validate(blueprint,json.loads(Path(args.schema).read_text()))
 print('Render JSON schema validation passed')
print('Deployment structure checks passed (remote deployment not tested)')
