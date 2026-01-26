import pytest

def is_active(evt, t):
    start = evt.get("time", 0)
    end = start + evt.get("duration", 0)
    return start <= t < end

@pytest.mark.parametrize(
    "evt,t,expected",
    [
        ({"time": 1.0, "duration": 2.0}, 0.99, False),
        ({"time": 1.0, "duration": 2.0}, 1.00, True),
        ({"time": 1.0, "duration": 2.0}, 2.99, True),
        ({"time": 1.0, "duration": 2.0}, 3.00, False),
    ],
)
def test_overlay_activation_window(evt, t, expected):
    assert is_active(evt, t) is expected
