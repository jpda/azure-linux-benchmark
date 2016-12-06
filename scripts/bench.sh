#!/bin/sh
# see https://github.com/tsuna/sysbench-tools/blob/master/runtests.sh

basePath="$1"
iterations=4
root="$basePath/azbench"
OUTDIR="$root/out"
now=$(date -u +"%Y%m%d")
time=$(date -u +"%H%M%S")
name=$(hostname)
runId=$(uuidgen)
resultBaseContainerUri="$2"
resultSas="$3"
postQueueUri="$4"
postQueueSas="$5"
mkdir -p $OUTDIR
exec >>$OUTDIR/run.log 2>&1

mkdir -p $root

threads=$(nproc)
size=2G
blksize=16K
devices='ssd'

apt-get install sysbench -y 2>&1

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

echo "STARTING testing...`date`"
modes='rndrd seqrd seqwr rndwr rndrw'
for mode in $modes; do
  for device in $devices; do
    cd $root
    exec >>$OUTDIR/$device/$size-$mode-$threads.log 2>&1
    echo "TESTING $device-$mode-$threads.txt `date`"
      for ((i=1;i<=$iterations;i++)); do
      echo "`date` start iteration $i"
      sysbench --test=fileio --file-total-size=$size --file-test-mode=$mode \
          --max-time=180 --max-requests=100000000 --num-threads=$threads \
          --init-rng=on --file-extra-flags=direct \
          --file-fsync-freq=0 --file-block-size=$blksize run 
      done
    echo "`date` DONE TESTING $mode-$threads"
  done
done

exec >>$OUTDIR/$device/CPU-$threads 2>&1
for ((i=1;i<=$iterations;i++)); do
  echo "start CPU iteration $i | `date`"
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
    blobUrl="$resultBaseContainerUri/$now/$name/$time-$runId/${f##*/}?$resultSas"
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