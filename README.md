# 🍕 BelissimoPizzaBot - Telegram Oshxona Boti

**BelissimoPizzaBot** — bu foydalanuvchiga turli menyulardan taomlar tanlash, buyurtma qilish va to‘lov chekini yuborish imkonini beruvchi zamonaviy Telegram bot hisoblanadi. Admin esa foydalanuvchilarning buyurtmalari va faoliyatini kuzatib borishi mumkin.

---

## 📋 Asosiy Xususiyatlar

- Kategoriya asosida menyular:
  - 🥗 Vegeterianlar
  - 🍖 Go'shtli taomlar
  - 🍔 FastFood
  - 🥬 GreenFood
  - 🧁 Desert
  - 🧊 Salqin ichimliklar
- Har bir kategoriya ichida taomlar ro'yxati va ularning narxlari
- 🛒 "Sotib olish" tugmasi orqali buyurtma qilish imkoniyati
- 📸 Foydalanuvchi karta to‘lovi chekining suratini yuklaydi
- 💾 Cheklar stream orqali saqlanadi va JSON formatda faylga yoziladi
- 👨‍💼 Admin foydalanuvchilar ro‘yxatini va ularning buyurtmalarini ko‘ra oladi
- 📊 Admin statistikasi: har kuni nechta buyurtma tushganini ko‘rsatadi

---

## 🛠 Texnologiyalar

- Til: `C#`
- Platforma: `.NET`
- Telegram API: `Telegram.Bot`
- Ma'lumotlar formati: `JSON`

---

## 📦 JSON Formati (Buyurtmalar uchun)

```json
Users [
  {
    "id": "automatic GUID",
    "username": "fsfdsdf",
    "fileUrl": "chek saqlangan manzil",
    "phoneNumber": "telefon raqami",
    "deliveryAddress": "yetkazish manzili",
    "price": "sotib olingan mahsulot narxi",
    "orderCreatedTime": "2025-04-30T18:30:00"
  }
]

