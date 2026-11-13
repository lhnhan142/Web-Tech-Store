"""
fetch_images.py
Tìm và tải ảnh sản phẩm bằng DuckDuckGo Images.

Cài đặt:
    pip install ddgs

Chạy thử 3 sản phẩm:
    python fetch_images.py --csv products.csv --limit 3

Chạy tất cả:
    python fetch_images.py --csv products.csv

CSV cần có:
    Name, ImageFile

Có thể có:
    MaSanPham hoặc Sku, ImageUrl
"""

import argparse
import csv
import os
import re
import sys
import time
import urllib.request
from pathlib import Path

try:
    from ddgs import DDGS
except ImportError:
    try:
        from duckduckgo_search import DDGS
    except ImportError:
        print("Chưa cài thư viện.")
        print("Chạy: pip install ddgs")
        sys.exit(1)


MIN_BYTES = 5000
MAX_BYTES = 15 * 1024 * 1024

UA = (
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) "
    "AppleWebKit/537.36 (KHTML, like Gecko) "
    "Chrome/120.0 Safari/537.36"
)


# =========================
# KIỂM TRA FILE ẢNH
# =========================

def is_image(data: bytes) -> bool:
    return (
        data.startswith(b"\xff\xd8\xff")
        or data.startswith(b"\x89PNG\r\n\x1a\n")
        or data[:6] in (b"GIF87a", b"GIF89a")
        or (
            data[:4] == b"RIFF"
            and data[8:12] == b"WEBP"
        )
    )


# =========================
# TẢI ẢNH
# =========================

def try_download(url: str, path: Path) -> bool:
    try:
        request = urllib.request.Request(
            url,
            headers={"User-Agent": UA}
        )

        with urllib.request.urlopen(
            request,
            timeout=25
        ) as response:

            data = response.read(MAX_BYTES + 1)

        if len(data) < MIN_BYTES:
            return False

        if len(data) > MAX_BYTES:
            return False

        if not is_image(data):
            return False

        path.parent.mkdir(
            parents=True,
            exist_ok=True
        )

        path.write_bytes(data)

        return True

    except Exception:
        return False


# =========================
# CHUẨN HÓA TÊN
# =========================

def norm(text: str) -> str:
    return re.sub(
        r"[^a-z0-9]",
        "",
        (text or "").lower()
    )


# =========================
# KIỂM TRA ẢNH ĐÚNG SẢN PHẨ
# =========================

def relevant(name: str, text: str) -> bool:

    hay = norm(text)

    words = [
        w for w in re.split(
            r"\s+",
            name.strip()
        )
        if w
    ]

    model = [
        norm(w)
        for w in words
        if re.search(r"\d", w)
        and len(norm(w)) >= 4
    ]

    # Nếu có mã model, ưu tiên mã model
    if model:

        return any(
            m in hay
            for m in model
        )

    # Nếu không có model thì kiểm tra tên
    plain = [
        norm(w)
        for w in words
        if len(norm(w)) >= 3
    ]

    if not plain:
        return True

    hit = sum(
        1 for w in plain
        if w in hay
    )

    return hit * 3 >= len(plain) * 2


# =========================
# TÌM KIẾM ẢNH
# =========================

def search_images(
    query: str,
    name: str,
    retries: int = 3
):

    for attempt in range(
        1,
        retries + 1
    ):

        try:

            results = DDGS().images(
                query,
                max_results=20
            )

            good = []

            for r in results:

                link = r.get("image")

                title = (
                    r.get("title") or ""
                )

                source = (
                    r.get("url") or ""
                )

                if link and relevant(
                    name,
                    title + " " + source + " " + link
                ):

                    good.append(link)

            return good

        except Exception as ex:

            wait = 8 * attempt

            print(
                f"  Lỗi tìm kiếm: {ex}"
            )

            print(
                f"  Chờ {wait}s "
                f"rồi thử lại "
                f"{attempt}/{retries}"
            )

            time.sleep(wait)

    return None


# =========================
# TẠO SQL UPDATE
# =========================

def sql_line(
    ma: str,
    url: str
) -> str:

    safe_url = url.replace(
        "'",
        "''"
    )

    safe_ma = ma.replace(
        "'",
        "''"
    )

    return (
        "UPDATE dbo.SanPhams "
        f"SET AnhDaiDien = N'{safe_url}' "
        f"WHERE MaSanPham = N'{safe_ma}';\n"
    )


# =========================
# ĐỌC CSV
# =========================

def read_csv(csv_path: Path):

    with csv_path.open(
        encoding="utf-8-sig",
        newline=""
    ) as f:

        return list(
            csv.DictReader(f)
        )


# =========================
# MAIN
# =========================

def main():

    ap = argparse.ArgumentParser()

    ap.add_argument(
        "--csv",
        default="products.csv"
    )

    ap.add_argument(
        "--out",
        default=str(
            Path(__file__).resolve().parents[2]
            / "wwwroot" / "images" / "products"
        )
    )

    ap.add_argument(
       "--sql",
       default=str(
           Path(__file__).resolve().parents[2]
           / "Database" / "update_anh.sql"
       )
   )

    ap.add_argument(
        "--prefix",
        default="/images/products/"
    )

    ap.add_argument(
        "--limit",
        type=int,
        default=0
    )

    ap.add_argument(
        "--delay",
        type=float,
        default=2.0
    )

    ap.add_argument(
        "--suffix",
        default="product"
    )

    ap.add_argument(
        "--force",
        action="store_true",
        help="Tải lại ảnh đã tồn tại"
    )

    args = ap.parse_args()

    # Đường dẫn tính từ thư mục chạy lệnh
    csv_path = Path(args.csv).resolve()

    output_dir = Path(
        args.out
    ).resolve()

    sql_path = Path(
        args.sql
    ).resolve()

    # Kiểm tra CSV
    if not csv_path.exists():

        print(
            "Không thấy file CSV:",
            csv_path
        )

        return 1

    try:

        rows = read_csv(csv_path)

    except Exception as ex:

        print(
            "Không đọc được CSV:",
            ex
        )

        return 1

    if not rows:

        print("CSV rỗng.")

        return 1

    # Tạo thư mục ảnh
    output_dir.mkdir(
        parents=True,
        exist_ok=True
    )

    sql = []

    failed = []

    ok = 0

    skipped = 0

    searches = 0

    # =========================
    # DUYỆT SẢN PHẨM
    # =========================

    for index, row in enumerate(
        rows,
        start=1
    ):

        name = (
            row.get("Name") or ""
        ).strip()

        fname = os.path.basename(
            (
                row.get("ImageFile")
                or ""
            ).strip()
        )

        ma = (
            row.get("MaSanPham")
            or row.get("Sku")
            or ""
        ).strip()

        if not name or not fname:

            print(
                f"Bỏ qua dòng {index}: "
                "thiếu Name hoặc ImageFile"
            )

            continue

        path = output_dir / fname

        website_path = (
            args.prefix.rstrip("/")
            + "/"
            + fname
        )

        # =========================
        # ẢNH ĐÃ TỒN TẠI
        # =========================

        if (
            path.exists()
            and path.stat().st_size > 1000
            and not args.force
        ):

            print(
                f"ĐÃ CÓ   {fname}"
            )

            skipped += 1

            if ma:

                sql.append(
                    sql_line(
                        ma,
                        website_path
                    )
                )

            continue

        # =========================
        # GIỚI HẠN TÌM KIẾM
        # =========================

        if (
            args.limit
            and searches >= args.limit
        ):

            print(
                "Đã đạt giới hạn:",
                args.limit
            )

            break

        saved = False

        # =========================
        # LINK ẢNH CÓ SẴN
        # =========================

        manual = (
            row.get("ImageUrl") or ""
        ).strip()

        if manual:

            if try_download(
                manual,
                path
            ):

                print(
                    f"OK       {fname} "
                    "<- link trong CSV"
                )

                saved = True

        # =========================
        # TÌM ẢNH DUCKDUCKGO
        # =========================

        if not saved:

            searches += 1

            query = (
                name + " " + args.suffix
            ).strip()

            links = search_images(
                query,
                name
            )

            if links is None:

                print(
                    f"LỖI TÌM  {name}"
                )

                failed.append(
                    f"{fname}\t{name}\ttìm kiếm lỗi"
                )

                time.sleep(
                    max(args.delay, 0)
                )

                continue

            for url in links:

                if try_download(
                    url,
                    path
                ):

                    print(
                        f"OK       {fname} "
                        f"<- {url}"
                    )

                    saved = True

                    break

            time.sleep(
                max(args.delay, 0)
            )

        # =========================
        # KẾT QUẢ
        # =========================

        if saved:

            ok += 1

            if ma:

                sql.append(
                    sql_line(
                        ma,
                        website_path
                    )
                )

            else:

                print(
                    "CẢNH BÁO:",
                    fname,
                    "thiếu MaSanPham/Sku"
                )

        else:

            print(
                f"KHÔNG CÓ {fname} ({name})"
            )

            failed.append(
                f"{fname}\t{name}\tkhông tải được ảnh"
            )

    # =========================
    # GHI FILE SQL
    # =========================

    with sql_path.open(
        "w",
        encoding="utf-8-sig"
    ) as f:

        f.write(
            "-- Cập nhật ảnh sản phẩm\n"
        )

        f.writelines(sql)

    # =========================
    # GHI FILE THẤT BẠI
    # =========================

    if failed:

        with open(
            "failed.txt",
            "w",
            encoding="utf-8-sig"
        ) as f:

            f.write(
                "\n".join(failed)
            )

            f.write("\n")

    # =========================
    # THỐNG KÊ
    # =========================

    print()

    print(
        "========== KẾT QUẢ =========="
    )

    print(
        f"Tải mới: {ok}"
    )

    print(
        f"Đã có sẵn: {skipped}"
    )

    print(
        f"Thất bại: {len(failed)}"
    )

    print(
        f"Lượt tìm: {searches}"
    )

    print(
        "Thư mục ảnh:",
        output_dir
    )

    print(
        "File SQL:",
        sql_path
    )

    if failed:

        print(
            "Danh sách thất bại: failed.txt"
        )

    return 0


if __name__ == "__main__":

    sys.exit(main())
