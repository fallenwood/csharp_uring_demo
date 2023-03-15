add_rules("mode.debug", "mode.release")

target("uring_reexport")
    set_kind("shared")
    add_files("src/*.c")
    add_includedirs("./thirdparty/liburing/src/include")
    add_includedirs("./thirdparty/liburing/src")
