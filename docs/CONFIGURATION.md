# Configuration and tuning

## Before tuning

Use **Relative Output Mode**. Pressure and speed filters run at OTD's `PostTransform` stage, after relative output has converted tablet positions into per-report movement deltas. Tablet Mapping runs at `PreTransform`, before that conversion, so OTD computes relative movement from the mapped tablet coordinates. These filters are not intended for Absolute Output Mode. Pressure remains unchanged in every filter.

Start with a comfortable, unaccelerated OTD sensitivity and test one filter at a time. Remove other acceleration filters while tuning. Two speed-based filters multiply their gains, which can make the result much stronger and less predictable than either filter alone.

## Pressure Sensitivity

This filter maps the current pen report's pressure to a movement gain. It is pressure-responsive rather than speed-responsive: moving quickly with light pressure does not trigger the high-pressure gain. The original pressure report is left intact.

| Option | Meaning |
| --- | --- |
| **Pressure start** | Raw pressure where the gain begins transitioning away from Minimum gain. Read your own tablet's pressure values in OTD's Tablet Debugger; device ranges differ. |
| **Pressure full scale** | Raw pressure where Maximum gain is reached. Pressure values beyond this point stay at the maximum end of the curve. |
| **Minimum gain** | Movement multiplier at the low end of the pressure curve. `1` preserves the original delta, below `1` reduces it, and above `1` increases it. |
| **Maximum gain** | Movement multiplier at the high end of the pressure curve. |
| **Response exponent** | Shapes the transition. `1` is linear; below `1` moves toward the high-end gain sooner; above `1` preserves the low-end gain longer. |
| **Reverse pressure response** | Swaps which gain is used at light and firm pressure. Enable this when a firmer press should steady aim instead of accelerating it. |

Pressure start and full scale are raw device units, not percentages. Choose a start near your ordinary gameplay pressure and a full-scale value you can reach comfortably, rather than assuming the device uses an 8191 maximum. If pressure is unavailable or always zero, use a speed-based filter instead.

## Windows EPP Approximation

This filter applies a bounded speed curve within OTD. It does not toggle, read, or duplicate the Windows **Enhance pointer precision** setting. Windows and games can process input differently (including raw-input paths), and the filter only sees OTD's transformed movement, so matching Windows exactly is not possible.

| Option | Meaning |
| --- | --- |
| **Acceleration start speed** | Relative output units per second below which movement stays at `1x`. Increasing this preserves a wider unaccelerated precision range. |
| **Full acceleration speed** | Output speed where Maximum gain is reached. Keep it above the start speed. |
| **Maximum gain** | Upper movement multiplier. At `2`, a report delta is doubled once full acceleration speed is reached. |
| **Response exponent** | Shapes the ramp between the two speed limits. `1` is linear, below `1` accelerates earlier, and above `1` delays more gain until faster motion. |
| **Speed smoothing (ms)** | Low-pass time constant for measured speed changes. `0` reacts immediately. Higher values reduce per-report jitter but make gain rise and fall more slowly. |

The speed thresholds are measured after OTD's Relative Output Mode scaling, in the relative units sent to the pointer backend. If you change that mode's sensitivity, retune the speed limits. This filter provides an OTD-side acceleration response even when the game uses raw mouse input; Windows' own pointer acceleration may not affect such games.

## Flick Acceleration

This filter is intended for fast target reacquisition and broad turns. Low-speed movement uses Precision gain; faster movement ramps toward Maximum flick gain. A sub-1 precision gain gives fine adjustments more control while a short, fast stroke receives additional travel.

| Option | Meaning |
| --- | --- |
| **Flick start speed** | Output speed where the gain starts moving from Precision gain toward Maximum flick gain. |
| **Full boost speed** | Output speed where the maximum multiplier is reached. Lower it for earlier boost; raise it to reserve the strongest gain for faster flicks. |
| **Precision gain** | Movement multiplier below Flick start speed. Use below `1` for slower fine aim or `1` to keep the base sensitivity unchanged. |
| **Maximum flick gain** | Upper movement multiplier at and above Full boost speed. This is not a turn-angle setting. |
| **Response exponent** | Controls how quickly the multiplier rises after Flick start speed. Below `1` gives earlier boost; above `1` concentrates boost near the fastest movement. |
| **Speed smoothing (ms)** | Smooths report-to-report speed changes. Keep it low for fast turns; too much smoothing delays the boost and its release. |

### A practical tuning pass

1. Set the game's sensitivity and OTD's relative sensitivity to a comfortable slow-aim baseline, then leave them fixed during this pass.
2. Start with Precision gain at `1`, a modest Maximum flick gain, and a Flick start speed above your ordinary tracking speed.
3. Test repeatable fast strokes. Lower Full boost speed if ordinary fast strokes do not gain enough; raise it if tracking begins to accelerate unintentionally.
4. Increase Maximum flick gain in small steps until a short stroke covers the desired larger angle. Lower Precision gain only after the fast-turn response feels right.
5. Use speed smoothing only to reduce visible/jittery gain changes. It is a response delay, not extra stability for the game camera.

A 180-degree turn cannot be set directly because gain is multiplied by movement and depends on game sensitivity, the game's input path, OTD mapping, and the stroke itself. Validate with the target game rather than relying on a universal preset.

## Tablet Mapping

Tablet Mapping changes the coordinate map before Relative Output Mode turns consecutive tablet positions into movement deltas. Unlike a radial correction, it supports both overall X/Y scaling and a position-dependent 2D displacement field. This lets you correct a repeatable N-shaped horizontal stroke by applying a vertical offset that varies across X, and lets the correction vary with Y when different bands of the tablet behave differently.

For normalized tablet coordinate $p=(u,v)$, mapping pivot $c$, axis scale $s$, and bilinearly interpolated correction grid $d(u,v)$, the output coordinate is:

$$
p' = c + (p-c) \odot s + d(u,v)
$$

The correction grid has 5 columns and 5 rows at 0%, 25%, 50%, 75%, and 100% of the full digitizer range. OTD then computes relative movement from changes in $p'$. A constant correction moves the mapped origin; differences between neighboring corrections change local sensitivity and bend the path.

| Option | Meaning |
| --- | --- |
| **Digitizer maximum X / Y** | In OTD's Tablet Debugger, move the pen around the tablet edges/corners and enter the largest raw X and Y readings shown. These are full digitizer limits, not active-area dimensions. |
| **Mapping center X / Y (%)** | Pivot for the overall axis scale. At 50%, scaling keeps the digitizer center fixed. |
| **Horizontal map scale** | Overall X scale around its pivot. `1` preserves the axis; above `1` expands it and below `1` compresses it. |
| **Vertical map scale** | Overall Y scale around its pivot. `1` preserves the axis; above `1` expands it and below `1` compresses it. |
| **Horizontal correction grid (25 values)** | Comma-separated X offsets, as fractions of full digitizer width. Row-major order: five X values from left to right for Y=0%, then Y=25%, 50%, 75%, and 100%. |
| **Vertical correction grid (25 values)** | Comma-separated Y offsets, as fractions of full digitizer height, in the same row-major order. This is usually the control to change when a horizontal stroke bends up and down. |

Correction values are fractions, not whole-number percentages: `0.01` means 1% of that digitizer axis, and `-0.01` means -1%. Values are limited to +/-0.10 (10%); an invalid value count or non-number resets that entire axis grid to neutral. Defaults are identity mapping: both scales at `1` and all correction samples at `0`.

### Built-in swipe calibration

Calibration is triggered by a tablet Auxiliary-button binding, so no stylus button is needed. In the tablet's Auxiliary button bindings, choose **SpatialAim Calibration Toggle** for the button you want to use. Assign it only to the trigger button; pressing it starts or finishes capture, and release has no action. The binding is tablet-scoped and signals the matching SpatialAim Tablet Mapping filter.

1. Add **SpatialAim Calibration Toggle** to the desired tablet Auxiliary button. Make sure the SpatialAim Tablet Mapping filter is enabled in Relative Output Mode for that tablet.
2. Press the assigned tablet button once. OTD reports that calibration is armed.
3. Draw the same natural horizontal swipe at least three times, lifting the pen between strokes. Each stroke must span at least 40% of digitizer X and contain enough reports to describe its curve. Keep the movement direction consistent and use the same tablet area.
4. Press the assigned tablet button again. The filter averages the strokes, compares their vertical path to the straight line between each stroke's endpoints, and applies the inverse of the average vertical deviation. It needs **Calibration swipes required** valid strokes; with the default of `3`, fewer strokes leave the current map unchanged.
5. Check the OTD notification/log for the resulting 25-value vertical correction grid. Calibration replaces the existing Vertical correction grid with the learned curve, repeated across all five Y rows; the Horizontal correction grid and overall scales are left untouched.

The learned mapping is applied immediately and saved automatically per tablet under your operating system's local application-data directory at `SpatialAim/Calibration`. To update the visible **Vertical correction grid** in the OTD tablet profile as well, copy the 25 comma-separated values after `VerticalCorrectionSamples=` in the daemon console and paste them into that field, then save the profile. For debugging, SpatialAim prints the binding press, calibration result, complete copyable grid, and exact storage path to the **OTD daemon process console**. This may be a separate terminal/process from the desktop GUI. The OTD notification reports success or failure. If the correction moves the path farther from straight, inspect the stroke orientation and reverse the correction signs. Repeat calibration after changing the active area or tablet orientation.

### Correcting an N-shaped horizontal stroke

1. Set both digitizer maxima correctly, both centers to 50%, both scales to `1`, and both correction grids to all zeroes.
2. Draw the same slow horizontal stroke several times through the tablet. Record the vertical deviation at X=0%, 25%, 50%, 75%, and 100%, relative to the desired straight path. Use percentages of the full digitizer height.
3. Enter the **opposite** of each measured deviation in the Vertical correction grid. For example, if the measured path is `0%, +2%, -3%, +2%, 0%`, use `0, -0.02, 0.03, -0.02, 0` for each of the five Y rows when that same N-shaped error occurs across the full tablet height.
4. If the error only occurs around one Y band, put the correction row in that band and taper adjacent rows toward zero. The five rows let the map vary with Y instead of applying one global curve.
5. Retest several parallel horizontal strokes and vertical strokes. Adjust in small increments; corrections affect nearby coordinates through bilinear interpolation.

For a stroke that is straight but consistently too short or long overall, adjust the relevant axis scale instead of the correction grid. Keep mapping changes fixed while measuring. The map is a static coordinate transform, not speed acceleration or hand tracking, and no universal grid can infer your personal arm path automatically. It may map coordinates outside the original digitizer range; this is useful in Relative Output Mode because OTD differences transformed positions into deltas.

## Interaction and troubleshooting

- **Movement feels unexpectedly fast:** verify that only one speed acceleration filter is enabled; also check for game-side or system-side acceleration.
- **Acceleration changes after adjusting tablet sensitivity:** expected, because speed thresholds are expressed in post-transform relative output units per second.
- **Pressure response is barely noticeable:** use Tablet Debugger to find the pressure range reached during normal play, then move Pressure start and Pressure full scale into that range.
- **Boost jitters around its threshold:** slightly increase Speed smoothing or move the start threshold away from ordinary tracking speed. More smoothing adds latency.
- **No useful response in Absolute Output Mode:** switch to Relative Output Mode; these filters intentionally operate on relative deltas.
