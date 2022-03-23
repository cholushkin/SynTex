rmdir /s /q Output
md Output
call synth_land_chunks.cmd
call synth_land_chunks_blured.cmd
call synth_land_chunks_sepia.cmd
call synth_land_chunks_noise.cmd
call db2table.cmd