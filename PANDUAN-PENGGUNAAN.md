# Panduan Singkat UjangKasir MVP

UjangKasir adalah aplikasi kasir desktop untuk toko retail kecil-menengah. Aplikasi dapat langsung dipakai setelah instalasi tanpa lisensi, aktivasi online, trial, subscription, atau pembatasan penggunaan.

## Login

1. Jalankan aplikasi.
2. Masukkan username `admin`.
3. Masukkan password `admin123`.
4. Klik `Login`.

Segera ganti data user default pada sprint manajemen user berikutnya.

## Produk dan Barcode

1. Buka menu `Produk`.
2. Klik `Tambah`.
3. Isi kode, nama, kategori, satuan, harga beli, harga jual, stok, dan minimum stok.
4. Klik `Simpan Produk`.
5. Pilih produk dari tabel.
6. Klik `Generate Barcode`, `Generate QR Code`, atau `Preview Barcode`.
7. Untuk uji scanner USB, isi bagian `Simulasi Scan`, lalu klik `Cari Barcode`.

## User dan Role

1. Login sebagai Owner/Admin.
2. Buka menu `Pengaturan`.
3. Klik `Tambah User`.
4. Isi username, nama lengkap, password, dan role.
5. Klik `Simpan User`.
6. Untuk edit user, pilih user dari tabel, klik `Edit`, ubah data, lalu simpan.
7. Untuk menonaktifkan user, pilih user lalu klik `Nonaktifkan`.

Catatan: user tidak dapat menonaktifkan akunnya sendiri.

## POS dan Checkout

1. Buka menu `Kasir`.
2. Fokuskan input `Scan Barcode` dengan `F1`.
3. Scan barcode atau ketik barcode lalu tekan `Enter`.
4. Atur qty di keranjang jika diperlukan.
5. Pilih metode bayar.
6. Untuk tunai, isi uang bayar.
7. Klik `BAYAR (F8)`.
8. Setelah sukses, ringkasan struk tampil di kanan bawah.
9. Klik `Cetak Struk` untuk mencetak ringkasan transaksi terakhir.

## Shift Kasir

1. Buka menu `Shift Kasir`.
2. Isi modal awal.
3. Klik `Buka Shift`.
4. Setelah transaksi selesai, isi uang aktual.
5. Klik `Tutup Shift`.
6. Pilih riwayat shift untuk melihat preview laporan.
7. Klik `Cetak Laporan` jika ingin mencetak laporan shift.

## Laporan

1. Buka menu `Laporan`.
2. Pilih tanggal mulai, tanggal akhir, dan jenis laporan.
3. Klik `Buat Laporan`.
4. Klik `Export Excel` untuk membuat file `.xlsx`.
5. Klik `Export PDF` untuk membuat file `.pdf`.

Jenis laporan MVP:

- Penjualan harian
- Penjualan per periode
- Produk terlaris
- Laporan kasir
- Stok hampir habis
- Laba kotor sederhana

## Backup dan Restore

1. Buka menu `Backup`.
2. Isi `BackupPath` atau gunakan default.
3. Klik `Backup Sekarang`.
4. File dibuat dengan format `backup-ujangkasir-yyyyMMdd-HHmmss.db`.
5. Untuk restore, pilih file `.db`.
6. Klik `Restore Database`.
7. Restart aplikasi setelah restore.

Restore hanya boleh dilakukan oleh role Owner atau Admin.

## Log Error

Jika terjadi error tak terduga, aplikasi menulis log ke:

```text
Logs/ujangkasir-yyyyMMdd.log
```

## Publish Windows

Publish folder Windows x64:

```powershell
dotnet publish UjangKasir.Desktop\UjangKasir.Desktop.csproj -c Release -p:PublishProfile=WindowsFolder
```

Output publish ada di:

```text
publish\windows
```
