#single proc
taskset -pc 0 $$
dd if=/dev/zero bs=1M count=2070 2> >(grep bytes >&2 ) | gzip -c > /dev/null
	2170552320 bytes (2.2 GB, 2.0 GiB) copied, 14.8308 s, 146 MB/s
for i in {1..2}; do dd if=/dev/zero bs=1M count=2070 2> >(grep bytes >&2 ) | gzip -c > /dev/null & done
	2170552320 bytes (2.2 GB, 2.0 GiB) copied, 29.9252 s, 72.5 MB/s
	2170552320 bytes (2.2 GB, 2.0 GiB) copied, 29.9939 s, 72.4 MB/s

#adjacent proc
taskset -pc 0,1 $$
for i in {1..2}; do dd if=/dev/zero bs=1M count=2070 2> >(grep bytes >&2 ) | gzip -c > /dev/null & done

#non-adjacent
taskset -pc 0,2 $$
for i in {1..2}; do dd if=/dev/zero bs=1M count=2070 2> >(grep bytes >&2 ) | gzip -c > /dev/null & done