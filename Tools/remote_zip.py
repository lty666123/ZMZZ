import io
import os
import re
import shutil
import sys
import urllib.request
import zipfile


class HttpRangeFile(io.RawIOBase):
    def __init__(self, url):
        self.url = url
        request = urllib.request.Request(url, method="HEAD")
        with urllib.request.urlopen(request) as response:
            self.size = int(response.headers["Content-Length"])
        self.pos = 0

    def readable(self):
        return True

    def seekable(self):
        return True

    def tell(self):
        return self.pos

    def seek(self, offset, whence=io.SEEK_SET):
        if whence == io.SEEK_SET:
            self.pos = offset
        elif whence == io.SEEK_CUR:
            self.pos += offset
        elif whence == io.SEEK_END:
            self.pos = self.size + offset
        else:
            raise ValueError("invalid whence")
        return self.pos

    def read(self, size=-1):
        if size is None or size < 0:
            size = self.size - self.pos
        if size <= 0 or self.pos >= self.size:
            return b""

        end = min(self.pos + size, self.size) - 1
        request = urllib.request.Request(
            self.url,
            headers={"Range": "bytes={}-{}".format(self.pos, end)},
        )

        with urllib.request.urlopen(request) as response:
            if response.status not in (200, 206):
                raise RuntimeError("unexpected HTTP status " + str(response.status))
            data = response.read()

        self.pos += len(data)
        return data


def open_remote_zip(url):
    remote = HttpRangeFile(url)
    return zipfile.ZipFile(remote)


def main():
    if len(sys.argv) < 3:
        print("usage: remote_zip.py list|extract|sample URL [OUTPUT_DIR] [ARG]")
        return 1

    action = sys.argv[1]
    url = sys.argv[2]

    with open_remote_zip(url) as archive:
        if action == "list":
            for info in archive.infolist():
                print("{}\t{}\t{}".format(
                    info.filename, info.file_size, info.compress_size))
            return 0

        if action == "sample":
            if len(sys.argv) < 4:
                print("sample requires OUTPUT_DIR")
                return 1

            output_dir = sys.argv[3]
            targets = {
                "Attack 1": 6,
                "Attack 2": 6,
                "Death": 4,
                "Hurt": 4,
                "Idle Battle": 10,
                "Walking": 10,
            }
            pattern = re.compile(
                r"/(" + "|".join(re.escape(name) for name in targets) +
                r")/(\d+)\.png$",
                re.IGNORECASE,
            )

            for info in archive.infolist():
                match = pattern.search(info.filename)
                if match is None:
                    continue

                folder = match.group(1)
                frame = int(match.group(2))
                step = targets[folder]

                if (frame - 1) % step != 0:
                    continue

                target = os.path.join(output_dir, info.filename)
                os.makedirs(os.path.dirname(target) or output_dir, exist_ok=True)

                with archive.open(info) as source, open(target, "wb") as destination:
                    shutil.copyfileobj(source, destination)

                print(target)

            return 0

        if action != "extract":
            print("unknown action: " + action)
            return 1

        if len(sys.argv) < 4:
            print("extract requires OUTPUT_DIR")
            return 1

        output_dir = sys.argv[3]
        pattern = sys.argv[4] if len(sys.argv) >= 5 else r"\.(png|gif|jpg|aseprite|ase)$"
        regex = re.compile(pattern, re.IGNORECASE)

        for info in archive.infolist():
            if info.is_dir() or not regex.search(info.filename):
                continue

            target = os.path.join(output_dir, info.filename)
            os.makedirs(os.path.dirname(target) or output_dir, exist_ok=True)

            with archive.open(info) as source, open(target, "wb") as destination:
                shutil.copyfileobj(source, destination)

            print(target)

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
