# Detail Preview Layout Improvements

## Changes Made

### Image Sizes
**Before:**
- Main Image: 200x150px
- NIR Image: 200x150px  
- Composite Cameras: 200x150px

**After:**
- Main Image: 280x210px (+40% larger)
- NIR Image: 280x210px (+40% larger)
- Composite Cameras: 140x105px (smaller, to fit 3 horizontally)

### Layout & Spacing
- Increased spacing between Main and NIR: 16px → 20px
- Increased spacing between NIR and Cameras: 32px → 40px
- Added margin between individual cameras: 16px

### Labels & Styling
**Main Image:**
- Label: "Main Image (Normal)" (more descriptive)
- Font: Size 13, SemiBold
- Border: 2px solid #666 with 2px corner radius

**NIR Image:**
- Label: "NIR Image" 
- Font: Size 13, SemiBold
- Border: 2px solid #bbb with 2px corner radius
- NO NIR placeholder: Red circle with "NO NIR" text (matches example)

**Composite Cameras:**
- Section Label: "Composite Cameras"
- Individual Labels: "Cam 1", "Cam 2", "Cam 3" (Line 1) or "Cam 4", "Cam 5", "Cam 6" (Line 2)
- Camera labels appear **above** each image
- Font: Size 11, SemiBold
- Border: 2px solid #666 with 2px corner radius
- Camera index badge: Top-right corner with semi-transparent black background

### ViewModel Logic (Unchanged)
The `DetailPreviewViewModel.cs` already correctly handles:
- Line 1: Shows Cam 1, 2, 3 (lines 88-93)
- Line 2: Shows Cam 4, 5, 6 (lines 95-99)

## Testing
1. Run application: `dotnet run --project ChronoView/ChronoView.csproj`
2. Click "Start" to begin monitoring
3. Double-click any row in Line 1 tab
   - **Verify**: Detail preview appears with Main Img, NIR Img (or NO NIR), and Cam 1, Cam 2, Cam 3
4. Double-click any row in Line 2 tab
   - **Verify**: Detail preview shows Cam 4, Cam 5, Cam 6
5. Check image sizes are noticeably larger and labels are clear

## Build Result
✅ Build succeeded with 16 warnings (pre-existing)
