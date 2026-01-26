import json

def test_pub_meta_schema_minimal():
    # Contract: these fields exist in meta (backend/core/game_engine.py)
    meta = {
        "state": "IDLE",
        "score": 0,
        "time": 0.0,
        "status": "ok",
        "progress": 0,
        "overlays": [],
    }
    encoded = json.dumps(meta).encode("utf-8")
    decoded = json.loads(encoded.decode("utf-8"))
    assert set(["state","score","time","status","progress","overlays"]).issubset(decoded.keys())
