# Test Thai Font Display

## Test Strings
If Thai font is working properly, you should see these characters clearly:

- Title: "ยืนยันการลบ" (Confirm Delete)
- Content: "ต้องการลบสนามใช่หรือไม่?" (Do you want to delete the court?)
- Buttons: "ลบ" (Delete), "ยกเลิก" (Cancel), "ตกลง" (OK)

## If you see squares (□□□) or question marks (???)
This means the font system cannot display Thai characters. The fallback fonts should help:

1. **Leelawadee UI** - Windows built-in Thai font
2. **Tahoma** - Basic Thai support
3. **Arial Unicode MS** - Comprehensive Unicode support

## Current Font Strategy
```
FontFamily="ms-appx:///Assets/Fonts/NotoSansThai-Regular.ttf#Noto Sans Thai, Leelawadee UI, Tahoma, Arial Unicode MS"
```

The system will try fonts in order:
1. Custom Noto Sans Thai (if file exists)
2. System Leelawadee UI (should work on Windows)
3. System Tahoma (basic fallback)
4. Arial Unicode MS (last resort)

## Testing
1. Run the app
2. Navigate to Court management
3. Click "ลบ" button on any court item
4. Check if dialog shows Thai text properly

## Expected Result
✅ Dialog title: "ยืนยันการลบ"
✅ Dialog content: "ต้องการลบสนาม..."
✅ Delete button: "ลบ"
✅ Cancel button: "ยกเลิก"
