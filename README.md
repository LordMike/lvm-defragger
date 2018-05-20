# lvm-defragger

This tool will create the necessary `pvmove` commands to defragment LVM2 segments. It does much the same as https://bisqwit.iki.fi/source/lvm2defrag.html. 

Given the current layout of a series of logical volumes, it will create `pvmove` commands to move segments, in order to ensure that all volumes consist of one segment.

## Usage

#### Dump the current LVM layout

Use the following command, `pvs -o segtype,lv_name,seg_start_pe,seg_size_pe,pvseg_all,pv_name --reportformat json` to get the JSON formatted layout of the current state of LVM. The JSON should be put in a file named `pvs.json`.

#### Run the tool

The tool will read from the json file, and then output commands

#### Run the commands

At your own leisure, run the `pvmove` commands.

## Notes

This was created very quickly. It may fail in many fun and exciting ways. 

* It has only been tested on a volume group with one physical volume (an `mdadm` array).
