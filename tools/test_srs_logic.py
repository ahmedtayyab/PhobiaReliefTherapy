"""Independent check of the SRS panic-score formula (UC-10)."""

def panic_score(current_hr, baseline_hr, hm_score, ts_score):
    delta = current_hr - baseline_hr
    score = (delta * 0.5) + (hm_score * 0.3) + (ts_score * 0.2)
    if score < 0:
        score = 0
    if delta > 36:
        score = max(score, 22)
    if delta > 32 and score < 10:
        score = max(score, 10)
    if score < 10:
        classification = "Calm Stress"
        action = "Continue Exposure"
        severe = False
    elif score < 18:
        classification = "Panic Detected"
        action = "Repeat Level"
        severe = False
    else:
        classification = "Severe Panic"
        action = "Stop / Safe Room"
        severe = True
    return score, classification, action, severe


def check(name, ok):
    print(("PASS" if ok else "FAIL") + "  " + name)
    return 0 if ok else 1


def main():
    failed = 0
    score, cls, action, severe = panic_score(76, 75, 0, 0)
    failed += check("calm score < 10", score < 10 and cls == "Calm Stress")

    score, cls, action, severe = panic_score(116, 75, 0, 0)
    failed += check("delta HR 41 forces severe", score >= 22 and severe and action == "Stop / Safe Room")

    score, cls, action, severe = panic_score(90, 75, 20, 50)
    failed += check("weighted mix can enter panic band", score >= 10)

    print("failed:", failed)
    raise SystemExit(failed)


if __name__ == "__main__":
    main()
