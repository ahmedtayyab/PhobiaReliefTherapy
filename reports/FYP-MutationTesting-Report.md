# Mutation Testing Assignment
**Course**: CS-4006: Software Testing
**FYP Title**: Phobia Relief Therapy
**Target Module**: SimulatedTherapyLogic.VitalsAnalyzer

---

## Task 1: Baseline Assessment

### 1.1 — Select Your Target Module
For this assignment, the `VitalsAnalyzer` module was selected. It evaluates a patient's simulated anxiety level (Mild, Moderate, Severe) during therapy sessions based on baseline Heart Rate (HR), current HR, and exposure duration. This module is the most safety-critical component of our biofeedback system, as incorrect thresholds could lead to improper therapy progression or failing to trigger a necessary intervention.

### 1.2 — Run Coverage Analysis
The `.NET` unit test suite was executed using `coverlet` and the HTML report was generated.

### 1.3 — Baseline Report Table

| Metric | Value (%) | Notes / Missed Items |
| :--- | :--- | :--- |
| Tool Used | Coverlet | |
| Line Coverage | 100% | All execution lines covered |
| Branch Coverage | 100% | All branches covered |
| Function Coverage | 100% | All methods covered |
| Total Test Count | 11 | xUnit Tests |
| Tests Passing | 100% | |

### 1.4 — Preliminary Analysis
The coverage numbers show a perfect 100% score for line and branch coverage. However, this only tells us that every block of code was executed at least once by our inputs. It does *not* guarantee that our tests actually verify the correctness of the boundary conditions. For instance, testing `diff >= 30` with `diff = 40` achieves branch coverage but completely misses the boundary value `diff = 30`, leaving the system highly vulnerable to off-by-one logical faults that coverage metrics are blind to.

---

## Task 2: The Mutation Run

### 2.1 to 2.3 — Executing the Mutation Run
The industry standard `dotnet-stryker` tool was executed against the `SimulatedTherapyLogic` module using our baseline test suite.

### 2.4 — Results Documentation Table

| Metric | Value | Significance |
| :--- | :--- | :--- |
| Total Mutants Generated | 53 | Total distinct code changes tested. |
| Mutants Killed | 30 | Faults your tests successfully detected. |
| Mutants Survived | 13 | Critical: Faults your tests completely missed. |
| Mutants Timed Out | 0 | Mutants causing infinite loops. |
| Equivalent/Ignored Mutants | 10 | Excluded from score calculation. |
| **Mutation Score %** | **69.77%** | $\frac{30}{43} \times 100$ |
| Baseline Line Coverage % | 100% | From Task 1. |
| Coverage–Score Gap | 30.23% | Difference between line coverage and mutation score. |

### 2.5 — Reflection Requirement
The baseline Mutation Score of 69.77% is unacceptable given the 100% code coverage. A Coverage-Score Gap of roughly 30 percentage points reveals that while the tests execute the code, their assertions are far too weak to detect subtle boundary shifts or logical replacements. The test suite appears to be weakest at the explicit boundary thresholds (e.g. testing `>` vs `>=`) and missing negative validations for boolean logic (`&&` vs `||`).

---

## Task 3: Mutant Analysis & Eradication

### ANALYSIS TEMPLATE — Mutant #17: ROR in AnalyzeHeartRate()

**[M1] Mutant Identification**
- **Mutant ID**: mutmut #17 (Stryker)
- **Source File**: `SimulatedTherapyLogic/VitalsAnalyzer.cs`
- **Function / Method**: `AnalyzeHeartRate()`
- **Line Number**: Line 19
- **Mutation Operator Class**: ROR (Relational Operator Replacement)

**[M2] The Mutation — Original vs. Mutated Code**
```csharp
// ORIGINAL
if (diff >= 30 && exposureSeconds > 15)

// MUTATED
if (diff > 30 && exposureSeconds > 15)
```

**[M3] Semantic Impact Analysis**
This mutation causes a patient whose heart rate difference is exactly 30 BPM to be incorrectly evaluated. Under the original system, a difference of exactly 30 BPM triggers a "Severe Anxiety" classification. After the mutation, it drops through the condition and incorrectly evaluates them as having "Moderate Anxiety". This could lead to the therapy system failing to intervene appropriately during a severe anxiety spike.

**[M4] Root-Cause: Why Did This Mutant Survive?**
The existing test case completely missed the boundary value:
```csharp
[Fact]
public void AnalyzeHeartRate_ReturnsSevereAnxiety()
{
    var result = _analyzer.AnalyzeHeartRate(70, 110, 20); // diff = 40
    Assert.Equal("Severe Anxiety", result);
}
```
Because it passed `diff = 40`, both the original code (`40 >= 30`) and mutated code (`40 > 30`) evaluated to `true`. The assertion failed to discriminate between the two logical states.

**[M5] The Mutant-Killing Test Case**
```csharp
[Fact]
public void AnalyzeHeartRate_Boundary_SevereAnxiety_Kills_ID17()
{
    // Target boundary diff = 30
    // Original: 30 >= 30 (True)
    // Mutant: 30 > 30 (False -> returns Moderate Anxiety)
    var result = _analyzer.AnalyzeHeartRate(70, 100, 20);
    Assert.Equal("Severe Anxiety", result);
}
```

**[M6] Verification**
After adding the test, Stryker reported Mutant #17 status changed from SURVIVED to KILLED.

---

### ANALYSIS TEMPLATE — Mutant #35: LCR in IsSessionValid()

**[M1] Mutant Identification**
- **Mutant ID**: mutmut #35 (Stryker)
- **Source File**: `SimulatedTherapyLogic/VitalsAnalyzer.cs`
- **Function / Method**: `IsSessionValid()`
- **Line Number**: Line 39
- **Mutation Operator Class**: LCR (Logical Connector Replacement)

**[M2] The Mutation — Original vs. Mutated Code**
```csharp
// ORIGINAL
if (totalExposureSeconds >= 60 && averageHr > 50)

// MUTATED
if (totalExposureSeconds >= 60 || averageHr > 50)
```

**[M3] Semantic Impact Analysis**
This mutation dangerously weakens the session validation logic. Originally, a session is only valid if *both* conditions are met. Under the mutation, if a patient undergoes therapy for a very short duration (e.g. 30 seconds) but their average HR is >50, the system will falsely log it as a valid, completed session. This breaks the data tracking requirements for the patient's exposure therapy.

**[M4] Root-Cause: Why Did This Mutant Survive?**
The existing test suite only tested the "Happy Path":
```csharp
[Fact]
public void IsSessionValid_ReturnsTrue()
{
    Assert.True(_analyzer.IsSessionValid(120, 80));
}
```
With `totalExposure=120` and `averageHr=80`, both conditions evaluate to `true`. Since `True && True` is `True`, and `True || True` is also `True`, the test passes under both original and mutated code. We failed to provide a negative test case.

**[M5] The Mutant-Killing Test Case**
```csharp
[Fact]
public void IsSessionValid_ShortExposure_ReturnsFalse_Kills_ID35()
{
    // Kills LCR && -> ||
    // Original: False && True -> False
    // Mutant: False || True -> True
    Assert.False(_analyzer.IsSessionValid(30, 80));
}
```

**[M6] Verification**
After adding the negative test case, Stryker successfully reported Mutant #35 as KILLED.

---

### ANALYSIS TEMPLATE — Mutant #52: ROR in NeedsIntervention()

**[M1] Mutant Identification**
- **Mutant ID**: mutmut #52 (Stryker)
- **Source File**: `SimulatedTherapyLogic/VitalsAnalyzer.cs`
- **Function / Method**: `NeedsIntervention()`
- **Line Number**: Line 56
- **Mutation Operator Class**: ROR (Relational Operator Replacement)

**[M2] The Mutation — Original vs. Mutated Code**
```csharp
// ORIGINAL
return currentHr >= maxSafeHr;

// MUTATED
return currentHr > maxSafeHr;
```

**[M3] Semantic Impact Analysis**
This mutation causes the system to miss the exact emergency threshold. If the patient's maximum safe HR is 150 BPM, and they hit exactly 150 BPM, the original system correctly triggers an intervention. The mutated system would wait until they hit 151 BPM. This 1-BPM delay in a medical/biofeedback context is a safety hazard.

**[M4] Root-Cause: Why Did This Mutant Survive?**
The test case was over-specified and missed the exact boundary:
```csharp
[Fact]
public void NeedsIntervention_ReturnsTrue()
{
    Assert.True(_analyzer.NeedsIntervention(160, 150));
}
```
By passing 160 vs 150, `160 >= 150` and `160 > 150` are both true, completely missing the boundary fault.

**[M5] The Mutant-Killing Test Case**
```csharp
[Fact]
public void NeedsIntervention_Boundary_ReturnsTrue_Kills_ID52()
{
    // Original: 150 >= 150 -> True
    // Mutant: 150 > 150 -> False
    Assert.True(_analyzer.NeedsIntervention(150, 150));
}
```

**[M6] Verification**
After implementing the boundary test, Stryker reported Mutant #52 as KILLED.

---

## Task 4: Final Mutation Score Improvement

### 4.1 — Re-Execute the Full Mutation Run
Stryker was re-executed with the new targeted boundary test cases in place.

### 4.2 — Before / After Comparison Table

| Metric | Before (Task 2) | After (Task 4) | Change |
| :--- | :--- | :--- | :--- |
| Mutation Score % | 69.77% | 79.07% | +9.3% |
| Mutants Killed | 30 | 34 | +4 |
| Mutants Survived | 13 | 9 | -4 |
| New Tests Added | — | 3 | +3 |

### 4.3 — Reflection
Adding three precisely targeted tests yielded a nearly 10-point improvement in the overall mutation score. The fact that the score did not hit 100% demonstrates that there are still surviving mutants (mostly off-by-one errors in other boundary paths that I did not target). The most surprising insight was how perfectly branch coverage can hide logical flaws; achieving 100% coverage provided a false sense of security while severe business logic flaws (`&&` -> `||`) remained undetected. This exercise has fundamentally changed how I will write tests for my FYP moving forward: I will no longer just write tests to achieve coverage, but will actively design tests targeting boundary values and negative pathways to expose potential faults.
