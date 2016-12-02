#!/bin/sh
# see https://github.com/tsuna/sysbench-tools/blob/master/runtests.sh

basePath="$1"
iterations=1
root="$basePath/azbench"
OUTDIR="$root/out"
now=$(date -u +"%Y%m%d")
name=$(hostname)
runId=$(uuidgen)
resultBaseContainerUri="$2"
resultSas="$3"
postQueueUri="$4"
postQueueSas="$5"
mkdir -p $OUTDIR
exec >>$OUTDIR/runlog 2>&1

mkdir -p $root

threads=$(nproc)
size=100M
blksize=16K
devices='ssd'

apt-get install sysbench -q 2>&1

parallel() {
  for device in $devices; do
    cd $root || exit
    mkdir -p "$OUTDIR/$device" || exit
    sysbench --test=fileio --file-total-size=$size "$@" &
  done
  for device in $devices; do
    wait
  done
}
parallel cleanup
parallel prepare

echo "`date` STARTING testing..."
modes='rndrd seqrd seqwr rndwr rndrw'
for mode in $modes; do
  for device in $devices; do
    cd $root
    exec >$OUTDIR/$device/$size-$mode-$threads 2>&1
    echo "`date` TESTING $device-$mode-$threads.txt"
      for i in $iterations; do
      echo "`date` start iteration $i"
      sysbench --test=fileio --file-total-size=$size --file-test-mode=$mode \
          --max-time=180 --max-requests=100000000 --num-threads=$threads \
          --init-rng=on --file-extra-flags=direct \
          --file-fsync-freq=0 --file-block-size=$blksize run 
      done
    echo "`date` DONE TESTING $mode-$threads"
  done
done

exec >$OUTDIR/$device/CPU-$threads 2>&1
for i in $iterations; do
  echo "`date` start CPU iteration $i"
  sysbench --test=cpu --num-threads=$threads --cpu-max-prime=50000 run
done
exec >>$OUTDIR/runlog 2>&1
# cleanup testing files
cd $root
sysbench --test=fileio --file-total-size=$size cleanup

# copy output to blob storage
for f in $(find $OUTDIR -name "*" -type f)
  do
    echo "$f"
    blobUrl="$resultBaseContainerUri/$now/$name/$runId/${f##*/}?$resultSas"
    echo $blobUrl
    curl -v --header "x-ms-blob-type: BlockBlob" -T $f $blobUrl 
done
# queue up deletion
queueUrl="$postQueueUri/messages?$postQueueSas"
message=$(hostname | base64)
queueMessage="<QueueMessage><MessageText>$message</MessageText></QueueMessage>"
curl -v -H "Content-Type: application/xml" -d $queueMessage -X POST $queueUrl

# cleanup
cd $root
rm -rf $root/*