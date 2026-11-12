#!/usr/bin/env bash
API=http://localhost:5057
KC=http://localhost:8080/realms/ticketly/protocol/openid-connect/token

tok() {
  curl -s -X POST "$KC" \
    -d grant_type=password -d client_id=ticketly-dev-cli \
    -d username="$1" -d password="$2" -d scope=openid \
  | python3 -c "import sys,json;print(json.load(sys.stdin)['access_token'])"
}

for u in alice bob carol; do
  echo "== $u"
  curl -s -H "Authorization: Bearer $(tok $u ${u}_dev_pw)" "$API/me"
  echo
done