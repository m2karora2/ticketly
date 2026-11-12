#!/usr/bin/env bash
API=http://localhost:5057
KC=http://localhost:8080/realms/ticketly/protocol/openid-connect/token

tok() {
  curl -s -X POST "$KC" \
    -d grant_type=password -d client_id=ticketly-dev-cli \
    -d username="$1" -d password="$2" -d scope=openid \
  | python3 -c "import sys,json;print(json.load(sys.stdin)['access_token'])"
}
id_of() { python3 -c "import sys,json;print(json.load(sys.stdin)['id'])"; }
create() {
  curl -s -X POST "$API/tickets" -H "Authorization: Bearer $1" \
    -H "Content-Type: application/json" \
    -d '{"title":"Auth test","description":"created by script"}'
}
code() { curl -s -o /dev/null -w "%{http_code}" -H "Authorization: Bearer $1" "$API/tickets/$2"; }

A=$(tok alice alice_dev_pw)
B=$(tok bob bob_dev_pw)
ALICE_TICKET=$(create "$A" | id_of)
BOB_TICKET=$(create "$B" | id_of)

echo "alice reads her own ticket   (expect 200): $(code "$A" "$ALICE_TICKET")"
echo "bob reads alice's ticket     (expect 200): $(code "$B" "$ALICE_TICKET")"
echo "alice reads bob's ticket     (expect 404): $(code "$A" "$BOB_TICKET")"
echo "no token creates a ticket    (expect 401): $(curl -s -o /dev/null -w "%{http_code}" -X POST "$API/tickets" -H 'Content-Type: application/json' -d '{"title":"x","description":"y"}')"